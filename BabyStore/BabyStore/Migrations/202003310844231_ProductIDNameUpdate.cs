namespace BabyStore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ProductIDNameUpdate : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.ProductImageMappings", new[] { "ProductId" });
            CreateIndex("dbo.ProductImageMappings", "ProductID");
        }
        
        public override void Down()
        {
            DropIndex("dbo.ProductImageMappings", new[] { "ProductID" });
            CreateIndex("dbo.ProductImageMappings", "ProductId");
        }
    }
}
