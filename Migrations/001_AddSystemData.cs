using Rock.Plugin;

namespace cc.newspring.AttendedCheckIn.Migrations
{
    [MigrationNumber( 1, "1.6.0" )]
    public class AddSystemData : Migration
    {
        public string PersonSNAttributeGuid = "8B562561-2F59-4F5F-B7DC-92B2BB7BB7CF";
        public string GroupSNAttributeGuid = "9210EC95-7B85-4D11-A82E-0B677B32704E";
        public string AttendedCheckinSiteGuid = "30FB46F7-4814-4691-852A-04FB56CC07F0";
        public string ChildhoodInformationCategoryGuid = "752DC692-836E-4A3E-B670-4325CD7724BF";

        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            // Attended Check-in Site
            RockMigrationHelper.AddSite( "Rock Attended Check-in", "Attended Check-In Site.", "CheckinPark", AttendedCheckinSiteGuid );
            RockMigrationHelper.AddLayout( AttendedCheckinSiteGuid, "Checkin", "Checkin", "", "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0" );

            var cssHeaderLink = "<link rel=\"stylesheet\" href=\"/Plugins/cc_newspring/AttendedCheckin/Styles/styles.css\">";
            var customSiteUpdates = @"UPDATE [Site] SET [IsSystem] = 0, [PageHeaderContent] = '{0}' WHERE [Guid] = '{1}'";

            Sql( string.Format( customSiteUpdates, cssHeaderLink, AttendedCheckinSiteGuid ) );

            // Attended Check-in root page (no blocks)
            RockMigrationHelper.AddPage( "", "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0", "Attended Check-in", "Screens for managing Attended Check-in", "32A132A6-63A2-4840-B4A5-23D80994CCBD", "" );
            Sql( @"
                DECLARE @PageId int = (SELECT [Id] FROM [Page] WHERE [Guid] = '32A132A6-63A2-4840-B4A5-23D80994CCBD')
                DECLARE @SiteId int = (SELECT [Id] FROM [Site] WHERE [Guid] = '30FB46F7-4814-4691-852A-04FB56CC07F0')
                DECLARE @SiteDomain varchar(200) = (SELECT TOP 1 [Domain] FROM [SiteDomain] WHERE [Domain] <> '')
                UPDATE [Site] SET [DefaultPageId] = @PageId, [AllowIndexing] = 0 WHERE [Id] = @SiteId

                /* Add the attended check-in route to the default domain */
                SELECT @SiteDomain = @SiteDomain + '/attendedcheckin'
                INSERT [SiteDomain] ([IsSystem], [SiteId], [Domain], [Guid])
                SELECT 0, @SiteId, @SiteDomain, NEWID()
            " );

            // Page: Admin
            RockMigrationHelper.AddPage( "32A132A6-63A2-4840-B4A5-23D80994CCBD", "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0", "Admin", "Admin screen for Attended Check-in", "771E3CF1-63BD-4880-BC43-AC29B4CCE963", "" ); // Site:Rock Attended Check-in
            RockMigrationHelper.AddPageRoute( "771E3CF1-63BD-4880-BC43-AC29B4CCE963", "attendedcheckin", "A7A8C510-F1A7-4F2D-A7B0-614CC8409837" );
            RockMigrationHelper.AddPageRoute( "771E3CF1-63BD-4880-BC43-AC29B4CCE963", "attendedcheckin/admin", "2EF42D59-D521-433B-BB0F-664633B5904F" );
            RockMigrationHelper.UpdateBlockType( "Check-in Administration", "Check-In Administration block", "~/Plugins/cc_newspring/AttendedCheckIn/Admin.ascx", "Check-in > Attended", "2C51230E-BA2E-4646-BB10-817B26C16218" );
            RockMigrationHelper.AddBlock( "771E3CF1-63BD-4880-BC43-AC29B4CCE963", "", "2C51230E-BA2E-4646-BB10-817B26C16218", "Admin", "Main", "", "", 0, "9F8731AB-07DB-406F-A344-45E31D0DE301" );
            RockMigrationHelper.AddBlockTypeAttribute( "2C51230E-BA2E-4646-BB10-817B26C16218", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 0, @"", "B196160E-4397-4C6F-8C5A-317CAD3C118F" );
            RockMigrationHelper.AddBlockTypeAttribute( "2C51230E-BA2E-4646-BB10-817B26C16218", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 0, @"", "7332D1F1-A1A5-48AE-BAB9-91C3AF085DB0" );
            RockMigrationHelper.AddBlockTypeAttribute( "2C51230E-BA2E-4646-BB10-817B26C16218", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"0", "18864DE7-F075-437D-BA72-A6054C209FA5" );
            RockMigrationHelper.AddBlockTypeAttribute( "2C51230E-BA2E-4646-BB10-817B26C16218", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 0, @"", "40F39C36-3092-4B87-81F8-A9B1C6B261B2" );
            RockMigrationHelper.AddBlockTypeAttribute( "2C51230E-BA2E-4646-BB10-817B26C16218", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Time to Cache Kiosk GeoLocation", "TimetoCacheKioskGeoLocation", "", "Time in minutes to cache the coordinates of the kiosk. A value of zero (0) means cache forever. Default 20 minutes.", 1, @"20", "F5512AB9-CDE2-46F7-82A8-99168D7784B2" );

            // Page: Search
            RockMigrationHelper.AddPage( "32A132A6-63A2-4840-B4A5-23D80994CCBD", "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0", "Search", "Search screen for Attended Check-in", "8F618315-F554-4751-AB7F-00CC5658120A", "" ); // Site:Rock Attended Check-in
            RockMigrationHelper.AddPageRoute( "8F618315-F554-4751-AB7F-00CC5658120A", "attendedcheckin/search", "7D190F34-C8B9-4B6E-AC11-C3B9BC207815" );
            RockMigrationHelper.UpdateBlockType( "Search", "Attended Check-In Search block", "~/Plugins/cc_newspring/AttendedCheckIn/Search.ascx", "Check-in > Attended", "645D3F2F-0901-44FE-93E9-446DBC8A1680" );
            RockMigrationHelper.AddBlock( "8F618315-F554-4751-AB7F-00CC5658120A", "", "645D3F2F-0901-44FE-93E9-446DBC8A1680", "Search", "Main", "", "", 0, "182C9AA0-E76F-4AAF-9F61-5418EE5A0CDB" );
            RockMigrationHelper.AddBlockTypeAttribute( "645D3F2F-0901-44FE-93E9-446DBC8A1680", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Admin Page", "AdminPage", "", "", 0, @"", "BBB93FF9-C021-4E82-8C03-55942FA4141E" );
            RockMigrationHelper.AddBlockTypeAttribute( "645D3F2F-0901-44FE-93E9-446DBC8A1680", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 0, @"", "72E40960-2072-4F08-8EA8-5A766B49A2E0" );
            RockMigrationHelper.AddBlockTypeAttribute( "645D3F2F-0901-44FE-93E9-446DBC8A1680", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 0, @"", "BF8AAB12-57A2-4F50-992C-428C5DDCB89B" );
            RockMigrationHelper.AddBlockTypeAttribute( "645D3F2F-0901-44FE-93E9-446DBC8A1680", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"0", "C4E992EA-62AE-4211-BE5A-9EEF5131235C" );
            RockMigrationHelper.AddBlockTypeAttribute( "645D3F2F-0901-44FE-93E9-446DBC8A1680", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 0, @"", "EBE397EF-07FF-4B97-BFF3-152D139F9B80" );
            RockMigrationHelper.AddBlockTypeAttribute( "645D3F2F-0901-44FE-93E9-446DBC8A1680", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Maximum Text Length", "MaximumTextLength", "", "Maximum length for text searches (defaults to 20).", 0, @"20", "970A9BD6-D58A-4F8E-8B20-EECB845E6BD6" );
            RockMigrationHelper.AddBlockTypeAttribute( "645D3F2F-0901-44FE-93E9-446DBC8A1680", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Minimum Text Length", "MinimumTextLength", "", "Minimum length for text searches (defaults to 4).", 0, @"4", "09536DD6-8020-400F-856C-DF3BEA6F76C5" );

            // Page: Family Select
            RockMigrationHelper.AddPage( "32A132A6-63A2-4840-B4A5-23D80994CCBD", "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0", "Family Select", "Family select for Attended Check-in", "AF83D0B2-2995-4E46-B0DF-1A4763637A68", "" ); // Site:Rock Attended Check-in
            RockMigrationHelper.AddPageRoute( "AF83D0B2-2995-4E46-B0DF-1A4763637A68", "attendedcheckin/family", "9DD23960-4EB7-45C5-9CDB-B1BA274FA4BF" );
            RockMigrationHelper.UpdateBlockType( "Family Select", "Attended Check-In Family Select Block", "~/Plugins/cc_newspring/AttendedCheckIn/FamilySelect.ascx", "Check-in > Attended", "4D48B5F0-F0B2-4C10-8498-DAF690761A80" );
            RockMigrationHelper.AddBlock( "AF83D0B2-2995-4E46-B0DF-1A4763637A68", "", "4D48B5F0-F0B2-4C10-8498-DAF690761A80", "Family Select", "Main", "", "", 0, "82929409-8551-413C-972A-98EDBC23F420" );
            RockMigrationHelper.AddBlock( "AF83D0B2-2995-4E46-B0DF-1A4763637A68", "", "49FC4B38-741E-4B0B-B395-7C1929340D88", "Idle Redirect", "Main", "", "", 1, "BDD502FF-40D2-42E6-845E-95C49C3505B3" );
            RockMigrationHelper.AddBlockTypeAttribute( "4D48B5F0-F0B2-4C10-8498-DAF690761A80", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 0, @"", "DD9F93C9-009B-4FA5-8FF9-B186E4969ACB" );
            RockMigrationHelper.AddBlockTypeAttribute( "4D48B5F0-F0B2-4C10-8498-DAF690761A80", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 0, @"", "81A02B6F-F760-4110-839C-4507CF285A7E" );
            RockMigrationHelper.AddBlockTypeAttribute( "4D48B5F0-F0B2-4C10-8498-DAF690761A80", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"0", "338CAD91-3272-465B-B768-0AC2F07A0B40" );
            RockMigrationHelper.AddBlockTypeAttribute( "4D48B5F0-F0B2-4C10-8498-DAF690761A80", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 0, @"", "2DF1D39B-DFC7-4FB2-B638-3D99C3C4F4DF" );

            // Page: Confirmation
            RockMigrationHelper.AddPage( "32A132A6-63A2-4840-B4A5-23D80994CCBD", "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0", "Confirmation", "Confirmation screen for Attended Check-in", "BE996C9B-3DFE-407F-BD53-D6F58D85A035", "" ); // Site:Rock Attended Check-in
            RockMigrationHelper.AddPageRoute( "BE996C9B-3DFE-407F-BD53-D6F58D85A035", "attendedcheckin/confirm", "0040D507-0325-4C52-951C-E37414A5FFDB" );
            RockMigrationHelper.UpdateBlockType( "Confirmation Block", "Attended Check-In Confirmation Block", "~/Plugins/cc_newspring/AttendedCheckIn/Confirm.ascx", "Check-in > Attended", "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F" );
            RockMigrationHelper.AddBlock( "BE996C9B-3DFE-407F-BD53-D6F58D85A035", "", "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "Confirmation", "Main", "", "", 0, "7CC68DD4-A6EF-4B67-9FEA-A144C479E058" );
            RockMigrationHelper.AddBlock( "BE996C9B-3DFE-407F-BD53-D6F58D85A035", "", "49FC4B38-741E-4B0B-B395-7C1929340D88", "Idle Redirect", "Main", "", "", 1, "FAEC5FCC-B850-4DA6-8844-715159D39BD5" );
            RockMigrationHelper.AddBlockTypeAttribute( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 0, @"", "E45D2B10-D1B1-4CBE-9C7A-3098B1D95F47" );
            RockMigrationHelper.AddBlockTypeAttribute( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 0, @"", "48813610-DD26-4E72-9D19-817535802C49" );
            RockMigrationHelper.AddBlockTypeAttribute( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"0", "2A71729F-E7CA-4ACD-9996-A6A661A069FD" );
            RockMigrationHelper.AddBlockTypeAttribute( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 0, @"", "DEB23724-94F9-4164-BFAB-AD2DDE1F90ED" );
            RockMigrationHelper.AddBlockTypeAttribute( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Activity Select Page", "ActivitySelectPage", "", "", 0, @"", "2D54A2C9-759C-45B6-8E23-42F39E134170" );
            RockMigrationHelper.AddBlockTypeAttribute( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Print Individual Labels", "PrintIndividualLabels", "", "", 0, @"False", "FB49557F-4B8E-4F7B-ACE1-A4230C3BB832" );
            RockMigrationHelper.AddBlockTypeAttribute( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 0, @"", "2D44DDDC-F0DD-49E1-8A56-7D8B979D9C67" );

            // Page: Activity Select
            RockMigrationHelper.AddPage( "32A132A6-63A2-4840-B4A5-23D80994CCBD", "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0", "Activity Select", "Activity select for Attended Check-in", "C87916FE-417E-4A11-8831-5CFA7678A228", "" ); // Site:Rock Attended Check-in
            RockMigrationHelper.AddPageRoute( "C87916FE-417E-4A11-8831-5CFA7678A228", "attendedcheckin/activity", "A48C7849-D4A4-41E7-BA1D-64A52F0D679E" );
            RockMigrationHelper.UpdateBlockType( "Activity Select", "Attended Check-In Activity Select Block", "~/Plugins/cc_newspring/AttendedCheckIn/ActivitySelect.ascx", "Check-in > Attended", "78E2AB4A-FDF7-4864-92F7-F052050BC4BB" );
            RockMigrationHelper.AddBlock( "C87916FE-417E-4A11-8831-5CFA7678A228", "", "78E2AB4A-FDF7-4864-92F7-F052050BC4BB", "Activity Select", "Main", "", "", 0, "8C8CBBE9-2502-4FEC-804D-C0DA13C07FA4" );
            RockMigrationHelper.AddBlock( "C87916FE-417E-4A11-8831-5CFA7678A228", "", "49FC4B38-741E-4B0B-B395-7C1929340D88", "Idle Redirect", "Main", "", "", 1, "31E6A1CC-2ABE-4ECC-B8DF-1FD2E8EBA203" );
            RockMigrationHelper.AddBlockTypeAttribute( "78E2AB4A-FDF7-4864-92F7-F052050BC4BB", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 0, @"", "6048A23D-6544-441A-A8B3-5782CAF5B468" );
            RockMigrationHelper.AddBlockTypeAttribute( "78E2AB4A-FDF7-4864-92F7-F052050BC4BB", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 0, @"", "39008E18-48C9-445F-B9D7-78334B76A7EE" );
            RockMigrationHelper.AddBlockTypeAttribute( "78E2AB4A-FDF7-4864-92F7-F052050BC4BB", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"0", "BEC10B87-4B19-4CD5-8952-A4D59DDA3E9C" );
            RockMigrationHelper.AddBlockTypeAttribute( "78E2AB4A-FDF7-4864-92F7-F052050BC4BB", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 0, @"", "5046A353-D901-45BB-9981-9CC1B33550C6" );

            // Admin Attributes
            RockMigrationHelper.AddBlockAttributeValue( "9F8731AB-07DB-406F-A344-45E31D0DE301", "18864DE7-F075-437D-BA72-A6054C209FA5", @"6E8CD562-A1DA-4E13-A45C-853DB56E0014" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "9F8731AB-07DB-406F-A344-45E31D0DE301", "40F39C36-3092-4B87-81F8-A9B1C6B261B2", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "9F8731AB-07DB-406F-A344-45E31D0DE301", "7332D1F1-A1A5-48AE-BAB9-91C3AF085DB0", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "9F8731AB-07DB-406F-A344-45E31D0DE301", "B196160E-4397-4C6F-8C5A-317CAD3C118F", @"00000000-0000-0000-0000-000000000000" ); // Previous Page

            // Search Attributes
            RockMigrationHelper.AddBlockAttributeValue( "182C9AA0-E76F-4AAF-9F61-5418EE5A0CDB", "C4E992EA-62AE-4211-BE5A-9EEF5131235C", @"6E8CD562-A1DA-4E13-A45C-853DB56E0014" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "182C9AA0-E76F-4AAF-9F61-5418EE5A0CDB", "BBB93FF9-C021-4E82-8C03-55942FA4141E", @"771E3CF1-63BD-4880-BC43-AC29B4CCE963,2EF42D59-D521-433B-BB0F-664633B5904F" ); // Admin Page
            RockMigrationHelper.AddBlockAttributeValue( "182C9AA0-E76F-4AAF-9F61-5418EE5A0CDB", "EBE397EF-07FF-4B97-BFF3-152D139F9B80", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "182C9AA0-E76F-4AAF-9F61-5418EE5A0CDB", "BF8AAB12-57A2-4F50-992C-428C5DDCB89B", @"AF83D0B2-2995-4E46-B0DF-1A4763637A68,9DD23960-4EB7-45C5-9CDB-B1BA274FA4BF" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "182C9AA0-E76F-4AAF-9F61-5418EE5A0CDB", "72E40960-2072-4F08-8EA8-5A766B49A2E0", @"BE996C9B-3DFE-407F-BD53-D6F58D85A035,0040D507-0325-4C52-951C-E37414A5FFDB" ); // Previous Page

            // Family Select Attributes
            RockMigrationHelper.AddBlockAttributeValue( "82929409-8551-413C-972A-98EDBC23F420", "338CAD91-3272-465B-B768-0AC2F07A0B40", @"6E8CD562-A1DA-4E13-A45C-853DB56E0014" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "82929409-8551-413C-972A-98EDBC23F420", "2DF1D39B-DFC7-4FB2-B638-3D99C3C4F4DF", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "82929409-8551-413C-972A-98EDBC23F420", "81A02B6F-F760-4110-839C-4507CF285A7E", @"BE996C9B-3DFE-407F-BD53-D6F58D85A035,0040D507-0325-4C52-951C-E37414A5FFDB" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "82929409-8551-413C-972A-98EDBC23F420", "DD9F93C9-009B-4FA5-8FF9-B186E4969ACB", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "BDD502FF-40D2-42E6-845E-95C49C3505B3", "2254B67B-9CB1-47DE-A63D-D0B56051ECD4", @"~/attendedcheckin/search" ); // New Location
            RockMigrationHelper.AddBlockAttributeValue( "BDD502FF-40D2-42E6-845E-95C49C3505B3", "1CAC7B16-041A-4F40-8AEE-A39DFA076C14", @"180" ); // Idle Seconds

            // Confirm Attributes
            RockMigrationHelper.AddBlockAttributeValue( "7CC68DD4-A6EF-4B67-9FEA-A144C479E058", "2A71729F-E7CA-4ACD-9996-A6A661A069FD", @"6E8CD562-A1DA-4E13-A45C-853DB56E0014" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "7CC68DD4-A6EF-4B67-9FEA-A144C479E058", "DEB23724-94F9-4164-BFAB-AD2DDE1F90ED", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "7CC68DD4-A6EF-4B67-9FEA-A144C479E058", "48813610-DD26-4E72-9D19-817535802C49", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "7CC68DD4-A6EF-4B67-9FEA-A144C479E058", "E45D2B10-D1B1-4CBE-9C7A-3098B1D95F47", @"AF83D0B2-2995-4E46-B0DF-1A4763637A68,9DD23960-4EB7-45C5-9CDB-B1BA274FA4BF" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "7CC68DD4-A6EF-4B67-9FEA-A144C479E058", "2D54A2C9-759C-45B6-8E23-42F39E134170", @"C87916FE-417E-4A11-8831-5CFA7678A228,A48C7849-D4A4-41E7-BA1D-64A52F0D679E" ); // Activity Select Page
            RockMigrationHelper.AddBlockAttributeValue( "FAEC5FCC-B850-4DA6-8844-715159D39BD5", "2254B67B-9CB1-47DE-A63D-D0B56051ECD4", @"~/attendedcheckin/search" ); // New Location
            RockMigrationHelper.AddBlockAttributeValue( "FAEC5FCC-B850-4DA6-8844-715159D39BD5", "1CAC7B16-041A-4F40-8AEE-A39DFA076C14", @"180" ); // Idle Seconds

            // Activity Attributes
            RockMigrationHelper.AddBlockAttributeValue( "8C8CBBE9-2502-4FEC-804D-C0DA13C07FA4", "BEC10B87-4B19-4CD5-8952-A4D59DDA3E9C", @"6E8CD562-A1DA-4E13-A45C-853DB56E0014" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "8C8CBBE9-2502-4FEC-804D-C0DA13C07FA4", "5046A353-D901-45BB-9981-9CC1B33550C6", @"8F618315-F554-4751-AB7F-00CC5658120A,7D190F34-C8B9-4B6E-AC11-C3B9BC207815" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "8C8CBBE9-2502-4FEC-804D-C0DA13C07FA4", "39008E18-48C9-445F-B9D7-78334B76A7EE", @"BE996C9B-3DFE-407F-BD53-D6F58D85A035,0040D507-0325-4C52-951C-E37414A5FFDB" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "8C8CBBE9-2502-4FEC-804D-C0DA13C07FA4", "6048A23D-6544-441A-A8B3-5782CAF5B468", @"BE996C9B-3DFE-407F-BD53-D6F58D85A035,0040D507-0325-4C52-951C-E37414A5FFDB" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "31E6A1CC-2ABE-4ECC-B8DF-1FD2E8EBA203", "2254B67B-9CB1-47DE-A63D-D0B56051ECD4", @"~/attendedcheckin/search" ); // New Location
            RockMigrationHelper.AddBlockAttributeValue( "31E6A1CC-2ABE-4ECC-B8DF-1FD2E8EBA203", "1CAC7B16-041A-4F40-8AEE-A39DFA076C14", @"180" ); // Idle Seconds

            // Custom workflow actions
            RockMigrationHelper.UpdateEntityType( "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByGender", "Filter Groups By Gender", "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByGender", false, true, "DC7DB1FD-8CC8-4AC4-B0A5-B5F85FF03540" );
            RockMigrationHelper.UpdateEntityType( "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit", "Select By Best Fit", "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit", false, true, "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944" );
            RockMigrationHelper.UpdateEntityType( "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByLastAttended", "Select By Last Attended", "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByLastAttended", false, true, "B4E27263-BB68-46DB-9876-D0E8C26449A3" );
            RockMigrationHelper.UpdateEntityType( "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended", "Select By Multiple Attended", "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended", false, true, "DDC2D0CA-28A9-420B-9915-B3831DE75DAC" );

            // Add special needs attributes so we can use them in the workflow
            // Person: Has Special Needs
            Sql( string.Format( @"
                INSERT INTO [dbo].[Attribute] ( [IsSystem],[FieldTypeId],[EntityTypeId],[EntityTypeQualifierColumn],[EntityTypeQualifierValue],[Key],[Name],[Description],[Order]
                    ,[IsGridColumn],[IsMultiValue],[IsRequired],[DefaultValue],[Guid] )
                VALUES (
                    0
                    ,(SELECT [Id] FROM [FieldType] WHERE [Class] = 'Rock.Field.Types.SelectSingleFieldType')
                    ,(SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.Person')
                    ,''
                    ,''
                    ,'HasSpecialNeeds'
                    ,'Has Special Needs'
                    ,'Flag to indicate if special needs are present'
                    ,0
                    ,0
                    ,0
                    ,0
                    ,'False'
                    ,'{0}')", PersonSNAttributeGuid ) );

            // Group: Has Special Needs
            Sql( string.Format( @"
                INSERT INTO [dbo].[Attribute] ( [IsSystem],[FieldTypeId],[EntityTypeId],[EntityTypeQualifierColumn],[EntityTypeQualifierValue],[Key],[Name],[Description],[Order]
                    ,[IsGridColumn],[IsMultiValue],[IsRequired],[DefaultValue],[Guid] )
                VALUES (
                    0
                    ,(SELECT [Id] FROM [FieldType] WHERE [Class] = 'Rock.Field.Types.SelectSingleFieldType')
                    ,(SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.Group')
                    ,''
                    ,''
                    ,'HasSpecialNeeds'
                    ,'Has Special Needs'
                    ,'Flag to indicate if special needs are present'
                    ,0
                    ,0
                    ,0
                    ,0
                    ,'False'
                    ,'{0}')", GroupSNAttributeGuid ) );

            Sql( string.Format( @"
                INSERT AttributeQualifier (IsSystem, AttributeId, [Key], Value, [Guid])
    			VALUES (
                    0
                    , (SELECT [Id] FROM [Attribute] WHERE [Guid] = '{0}')
                    , 'fieldtype'
                    , 'ddl'
                    , NEWID() )

				INSERT AttributeQualifier (IsSystem, AttributeId, [Key], Value, [Guid])
				VALUES(
                    0
                    , (SELECT [Id] FROM [Attribute] WHERE [Guid] = '{0}')
                    , 'values'
                    , 'Yes'
                    , NEWID() )", PersonSNAttributeGuid ) );

            Sql( string.Format( @"
                INSERT AttributeQualifier (IsSystem, AttributeId, [Key], Value, [Guid])
    			VALUES (
                    0
                    , (SELECT [Id] FROM [Attribute] WHERE [Guid] = '{0}')
                    , 'fieldtype'
                    , 'ddl'
                    , NEWID() )

				INSERT AttributeQualifier (IsSystem, AttributeId, [Key], Value, [Guid])
				VALUES(
                    0
                    , (SELECT [Id] FROM [Attribute] WHERE [Guid] = '{0}')
                    , 'values'
                    , 'Yes'
                    , NEWID() )", GroupSNAttributeGuid ) );

            // Make special needs appear under childhood info category
            Sql( string.Format( @"
                INSERT INTO [AttributeCategory]
                   ([AttributeId]
                   ,[CategoryId])
                VALUES
                   ((SELECT [Id] FROM [Attribute] WHERE [Guid] = '{0}')
                   ,(SELECT [Id] FROM [Category] WHERE [Guid] = '{1}'))
            ", PersonSNAttributeGuid, ChildhoodInformationCategoryGuid ) );

            RockMigrationHelper.AddGroupType( "Check in By Special Needs", "", "Group", "Member", false, true, true, "", 0, "0572A5FE-20A4-4BF1-95CD-C71DB5281392", 0, "6BCED84C-69AD-4F5A-9197-5C0F9C02DD34", "2CB16E13-141F-419F-BACD-8283AB6B3299", false );
            RockMigrationHelper.AddGroupTypeRole( "2CB16E13-141F-419F-BACD-8283AB6B3299", "Member", "", 0, null, null, "4DC318F0-5E6F-4F34-B3C5-08264B6DFD29", false );

            // Set workflow attribute defaults
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByAge:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "554108CF-31A1-47C0-A184-18B4A881D7FD" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByAge:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "6F98731B-1C17-49F0-8B5C-1C7DBDB08A07" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByAge:Remove
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove", "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", 0, @"False", "F05781E2-3517-4D20-A3BB-DA56CA025F25" );

            // Attended Check-in:Activity Search:Filter Groups By Special Needs:Active - false
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F940F6E5-6784-4925-8DF6-E59A911FDCBE", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "184D2EF3-247C-4C1F-93D6-49474CA95300" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:Order - false
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F940F6E5-6784-4925-8DF6-E59A911FDCBE", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "5F0C8E93-AB70-4508-999D-C5492E2ECADA" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:Remove - false
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F940F6E5-6784-4925-8DF6-E59A911FDCBE", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove", "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", 0, @"False", "F699D995-51EC-4EF7-8A03-B41D7C16A3C2" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:RemoveNonSN - false
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F940F6E5-6784-4925-8DF6-E59A911FDCBE", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove (or exclude) Non-Special Needs Groups", "RemoveNonSpecialNeedsGroups", "If set to true, non-special-needs groups will be removed if the person has special needs. This basically prevents special needs kids from getting put into regular classes. Default false.", 0, @"False", "2F00F31C-2042-43ED-BEB6-C051CA92DAC8" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:RemoveSN - false
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F940F6E5-6784-4925-8DF6-E59A911FDCBE", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove (or exclude) Special Needs Groups", "RemoveSpecialNeedsGroups", "If set to true, special-needs groups will be removed if the person is NOT special needs. This basically prevents non-special-needs kids from getting put into special needs classes. Default true.", 0, @"True", "9F72C20F-B927-4777-93A1-1EFEB8453960" );

            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "83F299E7-F2C9-4F0A-BA51-23D6CD0F9433" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "C599F69C-7295-4F82-A9A2-C769DBAF8765" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByLastAttended:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B4E27263-BB68-46DB-9876-D0E8C26449A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "8A1DBF48-1BF8-4EB8-9CDD-2D3773DD64EA" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByLastAttended:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B4E27263-BB68-46DB-9876-D0E8C26449A3", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "99840778-A814-4826-A976-46CC01CC2335" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "85BB0E2E-A8DE-4991-B92C-D6378F60001D" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "7237CFF5-BC59-4C2C-8314-F690038D93F8" );

            // Attended Workflow Type
            RockMigrationHelper.UpdateWorkflowType( false, true, "Attended Check-in", "Workflow for managing attended check-in", "8F8B272D-D351-485E-86D6-3EE5B7C84D99", "Check-in", "fa fa-list-ol", 0, false, 1, "6E8CD562-A1DA-4E13-A45C-853DB56E0014" ); // Attended Check-in
            RockMigrationHelper.UpdateWorkflowActivityType( "6E8CD562-A1DA-4E13-A45C-853DB56E0014", true, "Family Search", "", false, 0, "B6FC7350-10E0-4255-873D-4B492B7D27FF" ); // Attended Check-in:Family Search
            RockMigrationHelper.UpdateWorkflowActivityType( "6E8CD562-A1DA-4E13-A45C-853DB56E0014", true, "Person Search", "", false, 1, "6D8CC755-0140-439A-B5A3-97D2F7681697" ); // Attended Check-in:Person Search
            RockMigrationHelper.UpdateWorkflowActivityType( "6E8CD562-A1DA-4E13-A45C-853DB56E0014", true, "Activity Search", "", false, 2, "77CCAF74-AC78-45DE-8BF9-4C544B54C9DD" ); // Attended Check-in:Activity Search
            RockMigrationHelper.UpdateWorkflowActivityType( "6E8CD562-A1DA-4E13-A45C-853DB56E0014", true, "Save Attendance", "", false, 3, "BF4E1CAA-25A3-4676-BCA2-FDE2C07E8210" ); // Attended Check-in:Save Attendance
            RockMigrationHelper.UpdateWorkflowActivityType( "6E8CD562-A1DA-4E13-A45C-853DB56E0014", true, "Create Labels", "", false, 4, "21245E72-E914-4581-972E-06DD171DCE6A" ); // Attended Check-in:Create Labels

            // Family Search
            // Attended Check-in:Family Search:Find Families
            RockMigrationHelper.UpdateWorkflowActionType( "B6FC7350-10E0-4255-873D-4B492B7D27FF", "Find Families", 0, "E2F172A8-88E5-4F84-9805-73164516F5FB", true, false, "", "", 1, "", "A7690077-CCB7-4AB2-A945-7BEE4861AF9E" );

            // Person Search
            // Attended Check-in:Person Search:Find Family Members
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Find Family Members", 0, "5794B18B-8F43-43B2-8D60-6C047AB096AF", true, false, "", "", 1, "", "62D775D6-0689-43F9-AA16-858B77FAB87C" );
            // Attended Check-in:Person Search:Find Relationships
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Find Relationships", 1, "F43099A7-E872-439B-A750-351C741BCFEF", true, false, "", "", 1, "", "50019BF6-EF27-4F9B-A06F-6A185B5CBD39" );
            // Attended Check-in:Person Search:Load Group Types
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Load Group Types", 2, "50D5D915-074A-41FB-9EA7-0DBE52141398", true, false, "", "", 1, "", "05DD1385-B984-4905-8EA2-3B35EAC35B99" );
            // Attended Check-in:Activity Search:Load Groups
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Load Groups", 3, "008402A8-3A6C-4CB6-A230-6AD532505EDC", true, false, "", "", 1, "", "9DFD5255-CC94-4B88-AE2E-2FC32F35D9D9" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Filter Groups By Special Needs", 4, "F940F6E5-6784-4925-8DF6-E59A911FDCBE", true, false, "", "", 1, "", "ED2150E5-6B9E-4266-B0FA-9C6836C0DC20" );
            // Attended Check-in:Activity Search:Filter Groups By Grade
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Filter Groups By Grade", 5, "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", true, false, "", "", 1, "", "C250DB16-03C5-45C9-B70B-7A0DE7D5550E" );
            // Attended Check-in:Activity Search:Filter Groups By Age
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Filter Groups By Age", 6, "23F1E3FD-48AE-451F-9911-A5C7523A74B6", true, false, "", "", 1, "", "5F4CAF8E-AB49-409C-8831-845A51298A26" );
            // Attended Check-in:Activity Search:Remove Empty Group Types
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Remove Empty Group Types", 7, "E998B9A7-31C9-46F6-B91C-4E5C3F06C82F", true, false, "", "", 1, "", "9089B47B-B441-41DE-84A7-710F4E3E55EF" );
            // Attended Check-in:Person Search:Remove Empty People
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Remove Empty People", 8, "B8B72812-190E-4802-A63F-E693344754BD", true, false, "", "", 1, "", "1813C89A-623C-4234-91D4-3243CA68CD03" );

            // Activity Search
            // Attended Check-in:Activity Search:Load Locations
            RockMigrationHelper.UpdateWorkflowActionType( "77CCAF74-AC78-45DE-8BF9-4C544B54C9DD", "Load Locations", 1, "4492E36A-77C8-4DC7-8128-570FAA161ADB", true, false, "", "", 1, "", "1F342433-CE63-4FB4-88CE-00A8306ECED8" );
            // Attended Check-in:Activity Search:Filter Active Locations
            RockMigrationHelper.UpdateWorkflowActionType( "77CCAF74-AC78-45DE-8BF9-4C544B54C9DD", "Filter Active Locations", 2, "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", true, false, "", "", 1, "", "685CB9D2-EAA3-4322-81E3-289BFCAE15E7" );
            // Attended Check-in:Activity Search:Load Schedules
            RockMigrationHelper.UpdateWorkflowActionType( "77CCAF74-AC78-45DE-8BF9-4C544B54C9DD", "Load Schedules", 3, "24A7E196-B50B-4BD6-A347-07CFC5ABEF9E", true, false, "", "", 1, "", "E979A041-A2BE-4182-B478-43990D9BA185" );
            // Attended Check-in:Activity Search:Set Available Schedules
            RockMigrationHelper.UpdateWorkflowActionType( "77CCAF74-AC78-45DE-8BF9-4C544B54C9DD", "Set Available Schedules", 4, "0F16E0C5-825A-4058-8285-6370DAAC2C19", true, false, "", "", 1, "", "EADF5764-14E0-4956-AAA2-CED0DB5D5EC9" );
            // Attended Check-in:Activity Search:Select By Multiple Attended
            RockMigrationHelper.UpdateWorkflowActionType( "77CCAF74-AC78-45DE-8BF9-4C544B54C9DD", "Select By Multiple Attended", 5, "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", true, false, "", "", 1, "", "32CFAB16-629D-490C-A2C4-A95731BA5931" );
            // Attended Check-in:Activity Search:Select By Best Fit
            RockMigrationHelper.UpdateWorkflowActionType( "77CCAF74-AC78-45DE-8BF9-4C544B54C9DD", "Select By Best Fit", 6, "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", true, false, "", "", 1, "", "7D482C58-34CB-4414-9607-4BD01D0C217A" );

            // Save Attendance
            // Attended Check-in:Save Attendance:Save Attendance
            RockMigrationHelper.UpdateWorkflowActionType( "BF4E1CAA-25A3-4676-BCA2-FDE2C07E8210", "Save Attendance", 0, "50B2FEE6-DB7A-43C0-9DCF-19F61CD02BC6", true, false, "", "", 1, "", "93AF3357-7AE9-47AA-8B8B-C5351490E1ED" );

            // Attended Check-in:Save Attendance:Create Labels
            RockMigrationHelper.UpdateWorkflowActionType( "21245E72-E914-4581-972E-06DD171DCE6A", "Create Labels", 0, "8F348E7B-F9FD-4600-852D-477B13B0B4EE", true, false, "", "", 1, "", "BBE6E76D-6C8E-4B8E-931C-DD3CBE9619A4" );

            // Set attribute values
            // Attended Check-in:Family Search:Find Families:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "A7690077-CCB7-4AB2-A945-7BEE4861AF9E", "3404112D-3A97-4AE8-B699-07F62BD37D81", @"" );
            // Attended Check-in:Family Search:Find Families:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "A7690077-CCB7-4AB2-A945-7BEE4861AF9E", "1C6D8BD4-1A72-41E7-A9B5-AF37613058D8", @"False" );
            // Attended Check-in:Person Search:Find Family Members:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "62D775D6-0689-43F9-AA16-858B77FAB87C", "857A277E-6824-48FA-8E7A-9988AC4BCB13", @"" );
            // Attended Check-in:Person Search:Find Family Members:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "62D775D6-0689-43F9-AA16-858B77FAB87C", "3EF34D41-030B-411F-9D18-D331ABD89F0D", @"False" );
            // Attended Check-in:Person Search:Find Relationships:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "50019BF6-EF27-4F9B-A06F-6A185B5CBD39", "2C5535C6-80C9-4886-9A93-33A18F46AAA3", @"" );
            // Attended Check-in:Person Search:Find Relationships:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "50019BF6-EF27-4F9B-A06F-6A185B5CBD39", "6845038E-A08E-4D0A-BE1C-750034109496", @"False" );
            // Attended Check-in:Person Search:Load Group Types:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "05DD1385-B984-4905-8EA2-3B35EAC35B99", "1F4BD3F6-C528-4160-8478-825C3B8AC85D", @"" );
            // Attended Check-in:Person Search:Load Group Types:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "05DD1385-B984-4905-8EA2-3B35EAC35B99", "1C7CD28E-ACC5-4B88-BC05-E02D72919305", @"False" );
            // Attended Check-in:Person Search:Filter Groups by Grade:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "C250DB16-03C5-45C9-B70B-7A0DE7D5550E", "78A19926-CABA-475D-8AB9-F9EB08BD6FAA", @"" );
            // Attended Check-in:Person Search:Filter Groups by Grade:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "C250DB16-03C5-45C9-B70B-7A0DE7D5550E", "E69E09E8-C71D-4AA3-86AD-D46DBAE60816", @"False" );
            // Attended Check-in:Person Search:Filter Groups by Grade:Remove
            RockMigrationHelper.AddActionTypeAttributeValue( "C250DB16-03C5-45C9-B70B-7A0DE7D5550E", "39F82A06-DD0E-4D51-A364-8D6C0EB32BC4", @"False" );
            // Attended Check-in:Person Search:Filter Groups by Age:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "5F4CAF8E-AB49-409C-8831-845A51298A26", "554108CF-31A1-47C0-A184-18B4A881D7FD", @"" );
            // Attended Check-in:Person Search:Filter Groups by Age:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "5F4CAF8E-AB49-409C-8831-845A51298A26", "6F98731B-1C17-49F0-8B5C-1C7DBDB08A07", @"False" );
            // Attended Check-in:Person Search:Filter Groups by Age:Remove
            RockMigrationHelper.AddActionTypeAttributeValue( "5F4CAF8E-AB49-409C-8831-845A51298A26", "F05781E2-3517-4D20-A3BB-DA56CA025F25", @"False" );
            // Attended Check-in:Person Search:Remove Empty People:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "1813C89A-623C-4234-91D4-3243CA68CD03", "CFDAD883-5FAA-4EC6-B308-30BBB2EFAA94", @"" );
            // Attended Check-in:Person Search:Remove Empty People:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "1813C89A-623C-4234-91D4-3243CA68CD03", "EE892293-5B1E-4631-877E-179849F8D0FC", @"False" );
            // Attended Check-in:Activity Search:Load Groups:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "9DFD5255-CC94-4B88-AE2E-2FC32F35D9D9", "C26C5959-7144-443B-88ED-28E4A5AE544C", @"" );
            // Attended Check-in:Activity Search:Load Groups:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "9DFD5255-CC94-4B88-AE2E-2FC32F35D9D9", "AD7528AD-2A3D-4C26-B452-FA9F4F48953C", @"False" );
            // Attended Check-in:Activity Search:Load Groups:Load All
            RockMigrationHelper.AddActionTypeAttributeValue( "9DFD5255-CC94-4B88-AE2E-2FC32F35D9D9", "39762EF0-91D5-4B13-BD34-FC3AC3C24897", @"True" );
            // Attended Check-in:Activity Search:Load Locations:Load All
            RockMigrationHelper.AddActionTypeAttributeValue( "1F342433-CE63-4FB4-88CE-00A8306ECED8", "70203A96-AE70-47AD-A086-FD84792DF2B6", @"True" );
            // Attended Check-in:Activity Search:Load Locations:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "1F342433-CE63-4FB4-88CE-00A8306ECED8", "6EE6128C-79BF-4333-85DB-3B0C92B27131", @"" );
            // Attended Check-in:Activity Search:Load Locations:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "1F342433-CE63-4FB4-88CE-00A8306ECED8", "2F3B6B42-A89C-443A-9008-E9E96535E815", @"False" );
            // Attended Check-in:Activity Search:Load Schedules:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "E979A041-A2BE-4182-B478-43990D9BA185", "F7B09469-EB3D-44A4-AB8E-C74318BD4669", @"" );
            // Attended Check-in:Activity Search:Load Schedules:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "E979A041-A2BE-4182-B478-43990D9BA185", "4DFA9F8D-F2E6-4040-A23B-2A1F8258C767", @"False" );
            // Attended Check-in:Activity Search:Load Schedules:Load All
            RockMigrationHelper.AddActionTypeAttributeValue( "E979A041-A2BE-4182-B478-43990D9BA185", "B222CAF2-DF12-433C-B5D4-A8DB95B60207", @"True" );
            // Attended Check-in:Activity Search:Filter Active Locations:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "685CB9D2-EAA3-4322-81E3-289BFCAE15E7", "C8BE5BB1-9293-4FA0-B4CF-FED19B855465", @"" );
            // Attended Check-in:Activity Search:Filter Active Locations:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "685CB9D2-EAA3-4322-81E3-289BFCAE15E7", "D6BCB113-0699-4D58-8002-BC919CB4BA04", @"False" );
            // Attended Check-in:Activity Search:Filter Active Locations:Remove
            RockMigrationHelper.AddActionTypeAttributeValue( "685CB9D2-EAA3-4322-81E3-289BFCAE15E7", "885D28C5-A395-4A05-AEFB-6131498BDF12", @"True" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:Active - false
            RockMigrationHelper.AddActionTypeAttributeValue( "ED2150E5-6B9E-4266-B0FA-9C6836C0DC20", "184D2EF3-247C-4C1F-93D6-49474CA95300", @"False" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:Order - false
            RockMigrationHelper.AddActionTypeAttributeValue( "ED2150E5-6B9E-4266-B0FA-9C6836C0DC20", "5F0C8E93-AB70-4508-999D-C5492E2ECADA", @"" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:Remove - false
            RockMigrationHelper.AddActionTypeAttributeValue( "ED2150E5-6B9E-4266-B0FA-9C6836C0DC20", "F699D995-51EC-4EF7-8A03-B41D7C16A3C2", @"False" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:RemoveNonSN - false
            RockMigrationHelper.AddActionTypeAttributeValue( "ED2150E5-6B9E-4266-B0FA-9C6836C0DC20", "2F00F31C-2042-43ED-BEB6-C051CA92DAC8", @"False" );
            // Attended Check-in:Activity Search:Filter Groups By Special Needs:RemoveSN - true
            RockMigrationHelper.AddActionTypeAttributeValue( "ED2150E5-6B9E-4266-B0FA-9C6836C0DC20", "9F72C20F-B927-4777-93A1-1EFEB8453960", @"True" );
            // Attended Check-in:Activity Search:Remove Empty Groups:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "9089B47B-B441-41DE-84A7-710F4E3E55EF", "041E4A2B-90C6-4242-A7F1-ED07D9B348F2", @"" );
            // Attended Check-in:Activity Search:Remove Empty Groups:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "9089B47B-B441-41DE-84A7-710F4E3E55EF", "05C329B0-3794-42BD-9467-8F3FF95D7882", @"False" );
            // Attended Check-in:Activity Search:Select By Multiple Attended:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "32CFAB16-629D-490C-A2C4-A95731BA5931", "99840778-A814-4826-A976-46CC01CC2335", @"" );
            // Attended Check-in:Activity Search:Select By Multiple Attended:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "32CFAB16-629D-490C-A2C4-A95731BA5931", "8A1DBF48-1BF8-4EB8-9CDD-2D3773DD64EA", @"False" );
            // Attended Check-in:Activity Search:Select By Best Fit:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "7D482C58-34CB-4414-9607-4BD01D0C217A", "C599F69C-7295-4F82-A9A2-C769DBAF8765", @"" );
            // Attended Check-in:Activity Search:Select By Best Fit:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "7D482C58-34CB-4414-9607-4BD01D0C217A", "83F299E7-F2C9-4F0A-BA51-23D6CD0F9433", @"False" );
            // Attended Check-in:Save Attendance:Save Attendance:Security Code Length
            RockMigrationHelper.AddActionTypeAttributeValue( "93AF3357-7AE9-47AA-8B8B-C5351490E1ED", "D57F42C9-E497-4FEE-8231-4FE2D13DC191", @"3" );
            // Attended Check-in:Save Attendance:Save Attendance:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "93AF3357-7AE9-47AA-8B8B-C5351490E1ED", "3BDE9124-BB3F-4190-BECF-6510890649E4", @"" );
            // Attended Check-in:Save Attendance:Save Attendance:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "93AF3357-7AE9-47AA-8B8B-C5351490E1ED", "72A6C0DB-39C0-475B-A8EF-15A5D70FFA40", @"False" );
            // Attended Check-in:Save Attendance:Create Labels:Order
            RockMigrationHelper.AddActionTypeAttributeValue( "BBE6E76D-6C8E-4B8E-931C-DD3CBE9619A4", "F70112C9-4D93-41B9-A3FB-1E7C866AACCF", @"" );
            // Attended Check-in:Save Attendance:Create Labels:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "BBE6E76D-6C8E-4B8E-931C-DD3CBE9619A4", "36EB15CE-095C-41ED-9C0F-9EA345599D54", @"False" );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            // Remove special needs attribute from category
            Sql( string.Format( @"
                DELETE FROM [AttributeCategory] WHERE [AttributeId] = (SELECT [Id] FROM [Attribute] WHERE [Guid] = '{0}')
            ", PersonSNAttributeGuid ) );

            // Remove attibute
            RockMigrationHelper.DeleteAttribute( PersonSNAttributeGuid );

            // Delete Workflow Type
            Sql( @"DELETE [WorkflowType] WHERE [Guid] = '6E8CD562-A1DA-4E13-A45C-853DB56E0014'" );
            RockMigrationHelper.DeleteEntityType( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944" );
            RockMigrationHelper.DeleteEntityType( "B4E27263-BB68-46DB-9876-D0E8C26449A3" );
            RockMigrationHelper.DeleteEntityType( "DC7DB1FD-8CC8-4AC4-B0A5-B5F85FF03540" );
            RockMigrationHelper.DeleteAttribute( "1EFECBBD-0F94-4BFF-BE4D-C5B90082746D" );
            RockMigrationHelper.DeleteAttribute( "C908BE27-C2C9-4880-A755-D9983EEFE7E8" );
            RockMigrationHelper.DeleteAttribute( "AA84866B-F294-4209-890A-0901DE7C1B15" );
            RockMigrationHelper.DeleteAttribute( "83F299E7-F2C9-4F0A-BA51-23D6CD0F9433" );
            RockMigrationHelper.DeleteAttribute( "C599F69C-7295-4F82-A9A2-C769DBAF8765" );
            RockMigrationHelper.DeleteAttribute( "8A1DBF48-1BF8-4EB8-9CDD-2D3773DD64EA" );
            RockMigrationHelper.DeleteAttribute( "99840778-A814-4826-A976-46CC01CC2335" );

            // Delete Page: Activity Select
            RockMigrationHelper.DeleteAttribute( "5046A353-D901-45BB-9981-9CC1B33550C6" );
            RockMigrationHelper.DeleteAttribute( "BEC10B87-4B19-4CD5-8952-A4D59DDA3E9C" );
            RockMigrationHelper.DeleteAttribute( "39008E18-48C9-445F-B9D7-78334B76A7EE" );
            RockMigrationHelper.DeleteAttribute( "6048A23D-6544-441A-A8B3-5782CAF5B468" );
            RockMigrationHelper.DeleteBlock( "8C8CBBE9-2502-4FEC-804D-C0DA13C07FA4" );
            RockMigrationHelper.DeleteBlockType( "78E2AB4A-FDF7-4864-92F7-F052050BC4BB" );
            RockMigrationHelper.DeletePage( "C87916FE-417E-4A11-8831-5CFA7678A228" );
            RockMigrationHelper.DeleteAttribute( "02FB40A5-747B-441C-9825-2CA314DE6767" );
            RockMigrationHelper.DeleteAttribute( "AD90C10D-27B7-47C1-B07B-8074E94E4A5F" );
            RockMigrationHelper.DeleteBlock( "31E6A1CC-2ABE-4ECC-B8DF-1FD2E8EBA203" );

            // Delete Page: Confirmation
            RockMigrationHelper.DeleteAttribute( "FB49557F-4B8E-4F7B-ACE1-A4230C3BB832" );
            RockMigrationHelper.DeleteAttribute( "2D54A2C9-759C-45B6-8E23-42F39E134170" );
            RockMigrationHelper.DeleteAttribute( "DEB23724-94F9-4164-BFAB-AD2DDE1F90ED" );
            RockMigrationHelper.DeleteAttribute( "2A71729F-E7CA-4ACD-9996-A6A661A069FD" );
            RockMigrationHelper.DeleteAttribute( "48813610-DD26-4E72-9D19-817535802C49" );
            RockMigrationHelper.DeleteAttribute( "E45D2B10-D1B1-4CBE-9C7A-3098B1D95F47" );
            RockMigrationHelper.DeleteBlock( "7CC68DD4-A6EF-4B67-9FEA-A144C479E058" );
            RockMigrationHelper.DeleteBlockType( "5B1D4187-9B34-4AB6-AC57-7E2CF67B266F" );
            RockMigrationHelper.DeletePage( "BE996C9B-3DFE-407F-BD53-D6F58D85A035" );
            RockMigrationHelper.DeleteAttribute( "C1D6355B-2FF5-4FBB-AE91-1F76B43E4DB4" );
            RockMigrationHelper.DeleteAttribute( "BB5EF303-E4B5-43F9-978C-DE7B3946CA7D" );
            RockMigrationHelper.DeleteBlock( "FAEC5FCC-B850-4DA6-8844-715159D39BD5" );

            // Delete Page: Family Select
            RockMigrationHelper.DeleteAttribute( "2DF1D39B-DFC7-4FB2-B638-3D99C3C4F4DF" );
            RockMigrationHelper.DeleteAttribute( "338CAD91-3272-465B-B768-0AC2F07A0B40" );
            RockMigrationHelper.DeleteAttribute( "81A02B6F-F760-4110-839C-4507CF285A7E" );
            RockMigrationHelper.DeleteAttribute( "DD9F93C9-009B-4FA5-8FF9-B186E4969ACB" );
            RockMigrationHelper.DeleteAttribute( "A7F99980-BED4-4A80-AB83-DDAB5C7D7AAD" );
            RockMigrationHelper.DeleteAttribute( "C4204D6E-715E-4E3A-BA1B-949D20D26487" );
            RockMigrationHelper.DeleteBlock( "BDD502FF-40D2-42E6-845E-95C49C3505B3" );
            RockMigrationHelper.DeleteBlock( "82929409-8551-413C-972A-98EDBC23F420" );
            RockMigrationHelper.DeleteBlockType( "4D48B5F0-F0B2-4C10-8498-DAF690761A80" );
            RockMigrationHelper.DeletePage( "AF83D0B2-2995-4E46-B0DF-1A4763637A68" );

            // Delete Page: Search
            RockMigrationHelper.DeleteAttribute( "09536DD6-8020-400F-856C-DF3BEA6F76C5" );
            RockMigrationHelper.DeleteAttribute( "970A9BD6-D58A-4F8E-8B20-EECB845E6BD6" );
            RockMigrationHelper.DeleteAttribute( "EBE397EF-07FF-4B97-BFF3-152D139F9B80" );
            RockMigrationHelper.DeleteAttribute( "C4E992EA-62AE-4211-BE5A-9EEF5131235C" );
            RockMigrationHelper.DeleteAttribute( "BF8AAB12-57A2-4F50-992C-428C5DDCB89B" );
            RockMigrationHelper.DeleteAttribute( "72E40960-2072-4F08-8EA8-5A766B49A2E0" );
            RockMigrationHelper.DeleteAttribute( "BBB93FF9-C021-4E82-8C03-55942FA4141E" );
            RockMigrationHelper.DeleteBlock( "182C9AA0-E76F-4AAF-9F61-5418EE5A0CDB" );
            RockMigrationHelper.DeleteBlockType( "645D3F2F-0901-44FE-93E9-446DBC8A1680" );
            RockMigrationHelper.DeletePage( "8F618315-F554-4751-AB7F-00CC5658120A" );

            // Delete Page: Admin
            RockMigrationHelper.DeleteAttribute( "F5512AB9-CDE2-46F7-82A8-99168D7784B2" );
            RockMigrationHelper.DeleteAttribute( "40F39C36-3092-4B87-81F8-A9B1C6B261B2" );
            RockMigrationHelper.DeleteAttribute( "18864DE7-F075-437D-BA72-A6054C209FA5" );
            RockMigrationHelper.DeleteAttribute( "7332D1F1-A1A5-48AE-BAB9-91C3AF085DB0" );
            RockMigrationHelper.DeleteAttribute( "B196160E-4397-4C6F-8C5A-317CAD3C118F" );
            RockMigrationHelper.DeleteBlock( "9F8731AB-07DB-406F-A344-45E31D0DE301" );
            RockMigrationHelper.DeleteBlockType( "2C51230E-BA2E-4646-BB10-817B26C16218" );
            RockMigrationHelper.DeletePage( "771E3CF1-63BD-4880-BC43-AC29B4CCE963" );

            // Delete Page: Root
            Sql( @"UPDATE [Site] SET [DefaultPageId] = NULL WHERE [Guid] = '30FB46F7-4814-4691-852A-04FB56CC07F0'" );
            RockMigrationHelper.DeletePage( "32A132A6-63A2-4840-B4A5-23D80994CCBD" );

            // Delete Attended check-in site
            RockMigrationHelper.DeleteLayout( "3BD6CFC1-0BF2-43C8-AD38-44E711D6ACE0" );
            RockMigrationHelper.DeleteSite( AttendedCheckinSiteGuid );
        }
    }
}
