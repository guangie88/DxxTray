using Shaolinq;
using System;

namespace DxxTrayApp
{
    [DataAccessObject]
    public abstract class DirPath : DataAccessObject
    {
        [PersistedMember]
        public abstract string Value { get; set; }
    }

    [DataAccessModel]
    public abstract class DirPathModel : DataAccessModel
    {
        public abstract DataAccessObject<DirPath> DirPath { get; }
    }
}
