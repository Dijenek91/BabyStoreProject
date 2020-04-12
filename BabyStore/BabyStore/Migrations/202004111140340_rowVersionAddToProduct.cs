namespace BabyStore.Migrations.StoreConfigurations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rowVersionAddToProduct : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Products", "RowVersion");
        }
    }
}
