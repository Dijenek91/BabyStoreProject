namespace BabyStore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreateProductImageMappings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProductImageMappings",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        ImageNumber = c.Int(nullable: false),
                        ProductId = c.Int(nullable: false),
                        ProductImageID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .ForeignKey("dbo.ProductImages", t => t.ProductImageID, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.ProductImageID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProductImageMappings", "ProductImageID", "dbo.ProductImages");
            DropForeignKey("dbo.ProductImageMappings", "ProductId", "dbo.Products");
            DropIndex("dbo.ProductImageMappings", new[] { "ProductImageID" });
            DropIndex("dbo.ProductImageMappings", new[] { "ProductId" });
            DropTable("dbo.ProductImageMappings");
        }
    }
}
