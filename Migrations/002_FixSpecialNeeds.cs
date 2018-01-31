using Rock.Plugin;

namespace cc.newspring.AttendedCheckIn.Migrations
{
    [MigrationNumber( 2, "1.6.0" )]
    public class FixSpecialNeeds : Migration
    {
        public string PersonSNAttributeGuid = "8B562561-2F59-4F5F-B7DC-92B2BB7BB7CF";
        public string GroupSNAttributeGuid = "9210EC95-7B85-4D11-A82E-0B677B32704E";
        public string AttendedCheckinSiteGuid = "30FB46F7-4814-4691-852A-04FB56CC07F0";
        public string HasSpecialNeedsGroupTypeGuid = "2CB16E13-141F-419F-BACD-8283AB6B3299";

        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            // Limit Has Special Needs Attribute to Special Needs Groups
            Sql( string.Format( @"
                UPDATE Attribute
                SET EntityTypeQualifierColumn = 'GroupTypeId'
                    , EntityTypeQualifierValue = (SELECT Id FROM GroupType WHERE [Guid] = '{0}')
                WHERE [Guid] = '{1}'", HasSpecialNeedsGroupTypeGuid, GroupSNAttributeGuid ) );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            Sql( string.Format( @"
                UPDATE Attribute
                SET EntityTypeQualifierColumn = ''
                    , EntityTypeQualifierValue = ''
                WHERE [Guid] = '{0}'", GroupSNAttributeGuid ) );
        }
    }
}
