using System.Diagnostics;

namespace SenseNet.Security.Tests.TestPortal
{
    [DebuggerDisplay("{Index}:{Name}:'{Mask}'")]
    public class PermissionType : PermissionTypeBase
    {
        public PermissionType(string name, int index) : base(name, index) { }

        /// <summary>Index = 0</summary>
        public static readonly PermissionType FullControl = new PermissionType("FullControl", 0);
        /// <summary>Index = 1</summary>
        public static readonly PermissionType Modify = new PermissionType("Modify", 1);
        /// <summary>Index = 2</summary>
        public static readonly PermissionType ReadAndExecute = new PermissionType("ReadAndExecute", 2);
        /// <summary>Index = 3</summary>
        public static readonly PermissionType Read = new PermissionType("Read", 3);
        /// <summary>Index = 4</summary>
        public static readonly PermissionType Write = new PermissionType("Write", 4);
    }
}
