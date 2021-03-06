﻿namespace BabyStore
{
    public static class Constants
    {
        public const string BannerImagesPath = "~/Content/Banners/";
        public const string ProductImagePath = "~/Content/ProductImages/";
        public const string ProductThumbnailPath = "~/Content/ProductImages/Thumbnails/";
        public const int PageItems = 5;
        public const int MaxFileSize = 2097152;
        public const int NumberOfProductImages = 5;

        public static string[] allowedFileTypes = { ".gif", ".png", ".jpeg", ".jpg" };
    }
}