﻿using DxxTrayApp.Properties;
using System;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using NLog;

namespace DxxTray
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DxxTrayApp());
        }
    }

    public class DxxTrayApp : ApplicationContext
    {
        public DxxTrayApp()
        {
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
                const string DB_NAME = "DxxTrayApp.sqlite";

                if (!File.Exists(DB_NAME))
                {
                    SQLiteConnection.CreateFile(DB_NAME);
                }

                conn = new SQLiteConnection($"Data Source={DB_NAME};Version=3;");
                conn.Open();
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
                        new MenuItem("Add new entry", AddNewEntry),
                        new MenuItem("-"),
                        new MenuItem("Exit", Exit),
                    }),

                    Visible = true
                };

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

                okBtn = new Button()
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
                    Size = new Size(okBtn.Right + OUTER_PADDING * 2, descriptionLabel.Bottom + OUTER_PADDING),
                };

                // need to offset the titlebar height
                var screenRect = entryForm.RectangleToScreen(entryForm.ClientRectangle);
                entryForm.Height += screenRect.Top - entryForm.Top;

                entryForm.Controls.Add(picker);
                entryForm.Controls.Add(okBtn);
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
                okBtn.Click += (click_sender, click_e) =>
                {
                    // logic

                    // UX
                    entryForm.Close();
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

        // logger
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // engine fields
        private SQLiteConnection conn;

        // UI/UX fields
        private NotifyIcon trayIcon;
        private Form entryForm;
        private DateTimePicker picker;
        private Button okBtn;
    }
}