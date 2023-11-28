using System;
using System.Collections.Generic;
using System.Globalization;

namespace Solution.Parser.Project
{
    internal static class ProjectTypeParser
    {
        private static readonly Dictionary<Guid, ProjectType> ProjectTypeMapping = new()
        {
            { new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"), ProjectType.C_Sharp },
            { new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F"), ProjectType.VB_NET },
            { new Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942"), ProjectType.C_Plus_Plus },
            { new Guid("F2A71F9B-5D33-465A-A702-920D77279786"), ProjectType.F_Sharp },
            { new Guid("E6FDF86B-F3D1-11D4-8576-0002A516ECE8"), ProjectType.J_Sharp },
            { new Guid("262852C6-CD72-467D-83FE-5EEB1973A190"), ProjectType.J_Script },
            { new Guid("349C5851-65DF-11DA-9384-00065B846F21"), ProjectType.Web_Application },
            { new Guid("E24C65DC-7377-472B-9ABA-BC803B73C61A"), ProjectType.Web_Site },
            { new Guid("F135691A-BF7E-435D-8960-F99683D2D49C"), ProjectType.Distributed_System },
            { new Guid("3D9AD99F-2412-4246-B90B-4EAA41C64699"), ProjectType.WCF },
            { new Guid("60DC8134-EBA5-43B8-BCC9-BB4BC16C2548"), ProjectType.WPF },
            { new Guid("C252FEB5-A946-4202-B1D4-9916A0590387"), ProjectType.Visual_Database_Tools },
            { new Guid("A9ACE9BB-CECE-4E62-9AA4-C7E7C5BD2124"), ProjectType.Database },
            { new Guid("4F174C21-8C12-11D0-8340-0000F80270F8"), ProjectType.Database_Other_Project_Types },
            { new Guid("3AC096D0-A1C2-E12C-1390-A8335801FDAB"), ProjectType.Test },
            { new Guid("20D4826A-C6FA-45DB-90F4-C717570B9F32"), ProjectType.Legacy_2003_Smart_Device_C_Sharp },
            { new Guid("CB4CE8C6-1BDB-4DC7-A4D3-65A1999772F8"), ProjectType.Legacy_2003_Smart_Device_Vb_Net },
            { new Guid("4D628B5B-2FBC-4AA6-8C16-197242AEB884"), ProjectType.Smart_Device_C_Sharp },
            { new Guid("68B1623D-7FB9-47D8-8664-7ECEA3297D4F"), ProjectType.Smart_Device_Vb_Net },
            { new Guid("14822709-B5A1-4724-98CA-57A101D1B079"), ProjectType.Workflow_C_Sharp },
            { new Guid("D59BE175-2ED0-4C54-BE3D-CDAA9F3214C8"), ProjectType.Workflow_VB_Net },
            { new Guid("06A35CCD-C46D-44D5-987B-CF40FF872267"), ProjectType.Deployment_Merge_Module },
            { new Guid("3EA9E505-35AC-4774-B492-AD1749C4943A"), ProjectType.Deployment_Cab },
            { new Guid("978C614F-708E-4E1A-B201-565925725DBA"), ProjectType.Deployment_Setup },
            { new Guid("AB322303-2255-48EF-A496-5904EB18DA55"), ProjectType.Deployment_Smart_Device_Cab },
            {
                new Guid("A860303F-1F3F-4691-B57E-529FC101A107"), ProjectType.Visual_Studio_Tools_for_Applications
            },
            { new Guid("BAA0C2D2-18E2-41B9-852F-F413020CAA33"), ProjectType.Visual_Studio_Tools_for_Office },
            { new Guid("F8810EC1-6754-47FC-A15F-DFABD2E3FA90"), ProjectType.SharePoint_Workflow },
            { new Guid("6D335F3A-9D43-41b4-9D22-F6F17C4BE596"), ProjectType.XNA_Windows },
            { new Guid("2DF5C3F4-5A5F-47a9-8E94-23B4456F55E2"), ProjectType.XNA_XBox },
            { new Guid("D399B71A-8929-442a-A9AC-8BEC78BB2433"), ProjectType.XNA_Zune },
            { new Guid("EC05E597-79D4-47f3-ADA0-324C4F7C7484"), ProjectType.SharePoint_VB_Net },
            { new Guid("593B0543-81F6-4436-BA1E-4747859CAAE2"), ProjectType.SharePoint_C_Sharp },
            { new Guid("A1591282-1198-4647-A2B1-27E5FF5F6F3B"), ProjectType.Silverlight },
            { new Guid("603C0E0B-DB56-11DC-BE95-000D561079B0"), ProjectType.ASP_NET_MVC_1_0 },
            { new Guid("F85E285D-A4E0-4152-9332-AB1D724D3325"), ProjectType.ASP_NET_MVC_2_0 },
            { new Guid("E53F8FEA-EAE0-44A6-8774-FFD645390401"), ProjectType.ASP_NET_MVC_3_0 },
            { new Guid("E3E379DF-F4C6-4180-9B81-6769533ABE47"), ProjectType.ASP_NET_MVC_4_0 },
            { new Guid("82B43B9B-A64C-4715-B499-D71E9CA2BD60"), ProjectType.Extensibility },
            { new Guid("76F1466A-8B6D-4E39-A767-685A06062A39"), ProjectType.Store_App_Windows_Phone_8_1 },
            {
                new Guid("C089C8C0-30E0-4E22-80C0-CE093F111A43"),
                ProjectType.Store_App_Windows_Phone_8_1_Silverlight_C_Sharp
            },
            {
                new Guid("DB03555F-0C8B-43BE-9FF9-57896B3C5E56"),
                ProjectType.Store_App_Windows_Phone_8_1_Silverlight_VB_Net
            },
            { new Guid("BC8A1FFA-BEE3-4634-8014-F334798102B3"), ProjectType.Store_App_Windows_8_1 },
            { new Guid("D954291E-2A0B-460D-934E-DC6B0785DB48"), ProjectType.Store_App_Universal },
            { new Guid("786C830F-07A1-408B-BD7F-6EE04809D6DB"), ProjectType.Store_App_Portable_Universal },
            { new Guid("8BB0C5E8-0616-4F60-8E55-A43933E57E9C"), ProjectType.LightSwitch },
            { new Guid("581633EB-B896-402F-8E60-36F3DA191C85"), ProjectType.LightSwitch_Project },
            { new Guid("C1CDDADD-2546-481F-9697-4EA41081F2FC"), ProjectType.Office_SharePoint_App }
        };

        internal static ProjectType GetProjectType(Guid guid)
        {
            if (ProjectTypeMapping.TryGetValue(guid, out var value))
            {
                return value;
            }

            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                        "Unknown guid:'{0}' can not resolve project type", guid));
        }
    }
}
