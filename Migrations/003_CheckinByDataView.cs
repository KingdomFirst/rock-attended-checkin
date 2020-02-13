using Rock.Plugin;

namespace cc.newspring.AttendedCheckIn.Migrations
{
    [MigrationNumber( 3, "1.9.0" )]
    public class CheckinByDataView : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByDataView:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E6490F9B-21C6-4D0F-AD15-9729AC22C094", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "0B1FF2A0-D0CE-42A5-95BC-C65F02D14399" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByDataView:Remove
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E6490F9B-21C6-4D0F-AD15-9729AC22C094", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove", "Remove", "Select 'Yes' if groups should be removed.  Select 'No' if they should just be marked as excluded.", 1, @"True", "5EE8CC8E-455D-4115-88F4-81D224A30A28" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByDataView:DataView Group Attribute
            // RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E6490F9B-21C6-4D0F-AD15-9729AC22C094", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "DataView Group Attribute", "DataViewGroupAttribute", "Select the attribute used to filter by DataView.", 0, @"E8F8498F-5C51-4216-AC81-875349D6C2D0", "7BBDBA2B-CD6B-4631-B274-50942E2FB2CE" );
            // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.FilterGroupsByDataView:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E6490F9B-21C6-4D0F-AD15-9729AC22C094", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "869473D0-AC93-4F8F-B164-2A332571E779" );

            // Attended Check-in:Person Search:Filter Groups By Data View
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Filter Groups By Data View", 7, "E6490F9B-21C6-4D0F-AD15-9729AC22C094", true, false, "", "", 1, "", "A6D75C4C-F5DD-43DE-8403-96C6F35E225F" );
            
            // Change Order for new Action Type being added
            // Attended Check-in:Activity Search:Remove Empty Group Types
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Remove Empty Group Types", 8, "E998B9A7-31C9-46F6-B91C-4E5C3F06C82F", true, false, "", "", 1, "", "9089B47B-B441-41DE-84A7-710F4E3E55EF" );
            // Attended Check-in:Person Search:Remove Empty People
            RockMigrationHelper.UpdateWorkflowActionType( "6D8CC755-0140-439A-B5A3-97D2F7681697", "Remove Empty People", 9, "B8B72812-190E-4802-A63F-E693344754BD", true, false, "", "", 1, "", "1813C89A-623C-4234-91D4-3243CA68CD03" );

            // Attended Check-in:Person Search:Filter Groups By Data View:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "A6D75C4C-F5DD-43DE-8403-96C6F35E225F", "0B1FF2A0-D0CE-42A5-95BC-C65F02D14399", @"False" );
            // Attended Check-in:Person Search:Filter Groups By Data View:DataView Group Attribute
            //RockMigrationHelper.AddActionTypeAttributeValue( "A6D75C4C-F5DD-43DE-8403-96C6F35E225F", "7BBDBA2B-CD6B-4631-B274-50942E2FB2CE", @"e8f8498f-5c51-4216-ac81-875349d6c2d0" );
            // Attended Check-in:Person Search:Filter Groups By Data View:Remove
            RockMigrationHelper.AddActionTypeAttributeValue( "A6D75C4C-F5DD-43DE-8403-96C6F35E225F", "5EE8CC8E-455D-4115-88F4-81D224A30A28", @"False" );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            // Handled by the down method in AddSystemData of deleting the workflow type.
        }
    }
}
