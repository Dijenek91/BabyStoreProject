using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;

namespace BabyStore.FileOperations
{
    public static class FileHandler
    {
        public static bool ValidateFile(HttpPostedFileBase file)
        {
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
          
            if (file.ContentLength > 0 && file.ContentLength < Constants.MaxFileSize && Constants.allowedFileTypes.Contains(fileExtension))
            {
                return true;
            }

            return false;
        }

        public static void SaveFileToDisk(HttpPostedFileBase file)
        {
            WebImage img = new WebImage(file.InputStream);
            img.FileName = file.FileName;

            ResizeAndSaveImage(img);
            ResizeAndSaveThumbnail(img);
        }

     
        private static void ResizeAndSaveImage(WebImage img)
        {
            if (img.Width > 190)
            {
                img.Resize(190, img.Height);
            }
            img.Save(@Constants.ProductImagePath + Path.GetFileName(img.FileName)); 
        }

        private static void ResizeAndSaveThumbnail(WebImage img)
        {
            if (img.Width > 100)
            {
                img.Resize(100, img.Height);
            }

            img.Save(@Constants.ProductThumbnailPath + Path.GetFileName(img.FileName));//getFileName is mandatory since img.FileName will be changed after first save method
        }

    }
}