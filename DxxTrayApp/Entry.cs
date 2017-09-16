using Shaolinq;
using System;

namespace DxxTrayApp
{
    [DataAccessObject]
    public abstract class Entry : DataAccessObject<Guid>
    {
        [AutoIncrement]
        [PersistedMember]
        public abstract override Guid Id { get; set; }

        [PersistedMember]
        public abstract DateTime SubmitTime { get; set; }
    }

    [DataAccessModel]
    public abstract class EntryModel : DataAccessModel
    {
        [DataAccessObjects]
        public abstract DataAccessObjects<Entry> Entries { get; }
    }
}
