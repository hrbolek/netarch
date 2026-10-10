namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Stable identifiers used by the Digital Processes application.
/// </summary>
public static class DigitalProcessWellKnown
{
    /// <summary>
    /// Role type "já" assigned to the user who starts a Request.
    /// The role type is system data and grants the initiator's basic request access.
    /// </summary>
    public static readonly Guid InitiatorRoleTypeId =
        Guid.Parse("05a3e0f5-f71e-4caa-8012-229d868aa8ca");
    public static class FormNodeTypes
    {
        public static readonly Guid Section = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1001");
        public static readonly Guid Text = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1002");
        public static readonly Guid TextArea = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1003");
        public static readonly Guid Number = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1004");
        public static readonly Guid Date = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1005");
        public static readonly Guid Checkbox = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1006");
        public static readonly Guid Document = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1010");
        public static readonly Guid Export = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1009");
        public static readonly Guid Import = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1008");
        public static readonly Guid Select = Guid.Parse("7d80c90b-f7b3-45f2-a5c8-7e54d23d1007");
    }

}
