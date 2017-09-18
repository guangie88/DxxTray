using DxxTrayApp;
using DxxTrayApp.Properties;
using NLog;
using Shaolinq;
using Shaolinq.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Nett;
using System.IO;

namespace DxxTray
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var configFilePath = (args.Length == 0)
                ? "DxxTrayApp.toml"
                : args[0];

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DxxTrayApp(configFilePath));
        }
    }

    public class DxxTrayApp : ApplicationContext
    {
        const string DB_NAME = "DxxTrayApp.sqlite";
        readonly TimeSpan DEFAULT_EXPIRY_TIME = TimeSpan.FromDays(5);

        public DxxTrayApp(string configFilePath)
        {
            this.configFilePath = configFilePath;

            LogInit();
            EngineInit();
            UxInit();
            EventHandlingInit();
        }

        private void LogInit()
        {
            logger.Info("DxxTrayApp started...");
        }

        private void EngineInit()
        {
            try
            {
                entryModel = DataAccessModel.BuildDataAccessModel<EntryModel>(SqliteConfiguration.Create(DB_NAME));
                entryModel.Create(DatabaseCreationOptions.DeleteExistingDatabase);
            }
            catch (Exception e)
            {
                logger.Error($"Engine init error: {e.Message}");
                ExitImpl();
            }
        }

        private void UxInit()
        {
            try
            {
                // initialize tray icon

                trayIcon = new NotifyIcon()
                {
                    Icon = Resources.AppIcon,

                    ContextMenu = new ContextMenu(new MenuItem[]
                    {
                        new MenuItem("Add &new entry...", AddNewEntry),
                        new MenuItem("-"),
                        new MenuItem("&Set storing directory...", SetStoringDir),
                        new MenuItem("-"),
                        new MenuItem("E&xit", Exit),
                    }),

                    Visible = true
                };

                initialItemCount = trayIcon.ContextMenu.MenuItems.Count;

                // initialize entry form

                const int OUTER_PADDING = 15;
                const int INNER_PADDING = 8;

                // controls initialization
                picker = new DateTimePicker()
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "yyyy/MM/dd hh:mm",
                    Left = OUTER_PADDING,
                    Top = OUTER_PADDING,
                };

                confirmBtn = new Button()
                {
                    Text = "Confirm",
                    AutoSize = true,
                    Left = picker.Right + INNER_PADDING,
                    Top = picker.Top - 1,
                };

                var descriptionLabel = new Label()
                {
                    Text = "Format: yyyy/MM/dd hh:mm",
                    AutoSize = true,
                    Left = picker.Left,
                    Top = picker.Bottom + INNER_PADDING,
                };

                // form initialization
                entryForm = new Form()
                {
                    Text = "Entry Date/Time",
                    MinimizeBox = false,
                    MaximizeBox = false,
                    Size = new Size(confirmBtn.Right + OUTER_PADDING * 2, descriptionLabel.Bottom + OUTER_PADDING),
                };

                // need to offset the titlebar height
                var screenRect = entryForm.RectangleToScreen(entryForm.ClientRectangle);
                entryForm.Height += screenRect.Top - entryForm.Top;

                entryForm.Controls.Add(picker);
                entryForm.Controls.Add(confirmBtn);
                entryForm.Controls.Add(descriptionLabel);
            }
            catch (Exception e)
            {
                logger.Error($"UX init error: {e.Message}");
                ExitImpl();
            }
        }

        private void EventHandlingInit()
        {
            try
            {
                // enable event handling
                confirmBtn.Click += (sender, e) =>
                {
                    // logic
                    using (var scope = new DataAccessScope())
                    {
                        var entry = entryModel.Entries.Create();
                        entry.SubmitTime = picker.Value;

                        logger.Debug($"New entry (Id: {entry.Id}, SubmitTime: {entry.SubmitTime})");

                        var newMenuItem = new MenuItem($"{entry.SubmitTime} - {entry.Id}");
                        newMenuItem.Click += (inner_sender, inner_e) => entryMenuItems.Remove(newMenuItem);
                        entryMenuItems.Add(newMenuItem);

                        scope.Complete();
                    }

                    // UX
                    entryForm.Close();
                };

                entryMenuItems.CollectionChanged += (sender, e) =>
                {
                    // UX
                    logger.Debug($"Action: {e.Action}");

                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        var newMenuItems =
                            (from entry in e.NewItems.Cast<MenuItem>()
                            select entry).ToArray();

                        if (trayIcon.ContextMenu.MenuItems.Count == initialItemCount && newMenuItems.Length > 0)
                        {
                            // append a dash across the items when there is one item
                            trayIcon.ContextMenu.MenuItems.Add(0, new MenuItem("-"));
                        }

                        foreach (var newMenuItem in newMenuItems)
                        {
                            trayIcon.ContextMenu.MenuItems.Add(entryMenuItems.Count - 1, newMenuItem);
                            logger.Debug($"> Added: {newMenuItem}");
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Remove)
                    {
                        var deletedMenuItems =
                            from entry in e.OldItems.Cast<MenuItem>()
                            select entry;

                        foreach (var deletedMenuItem in deletedMenuItems)
                        {
                            trayIcon.ContextMenu.MenuItems.Remove(deletedMenuItem);
                            logger.Debug($"> Removed: {deletedMenuItem}");
                        }

                        // +1 because of the added dash item
                        if (trayIcon.ContextMenu.MenuItems.Count == initialItemCount + 1)
                        {
                            // delete away the dash if there are no more items
                            trayIcon.ContextMenu.MenuItems.RemoveAt(0);
                        }
                    }
                };
            }
            catch (Exception e)
            {
                logger.Error($"Event handling error: {e.Message}");
                ExitImpl();
            }
        }

        private void AddNewEntry(object sender, EventArgs e)
        {
            try
            {
                // always refresh the date time to current time
                if (picker != null)
                {
                    picker.Value = DateTime.Now;
                }

                entryForm.ShowDialog();
            }
            catch (Exception)
            {
                // can happen if the form was already shown
                // nothing to do
                entryForm.Focus();
            }
        }

        private void SetStoringDir(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // logic
                using (var scope = new DataAccessScope())
                {
                    var config = ReadOrCreateConfig();
                    config.StoreDirPath = dialog.SelectedPath;
                    Toml.WriteFile(config, configFilePath);

                    scope.Complete();
                }
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            ExitImpl();
        }

        private void ExitImpl()
        {
            // hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;
            Application.Exit();
        }

        private Configuration ReadOrCreateConfig()
        {
            if (!File.Exists(configFilePath))
            {
                using (var configStream = File.Create(configFilePath))
                {
                    var config = new Configuration()
                    {
                        StoreDirPath = "%HOMEPATH%",
                        ExpiryTime = DEFAULT_EXPIRY_TIME,
                    };

                    Toml.WriteStream(config, configStream);
                }
            }

            return Toml.ReadFile<Configuration>(configFilePath);
        }

        // logger
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // engine fields
        private string configFilePath;
        private EntryModel entryModel;
        private ObservableCollection<MenuItem> entryMenuItems = new ObservableCollection<MenuItem>();

        // UI/UX fields
        private NotifyIcon trayIcon;
        private int initialItemCount;
        private Form entryForm;
        private DateTimePicker picker;
        private Button confirmBtn;
    }
}