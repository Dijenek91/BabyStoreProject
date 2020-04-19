using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using BabyStore.Models.BabyStoreModelClasses;

namespace BabyStore.FileOperations
{
    public static class FileHandler
    {
        public static void SaveFileToDisk(HttpPostedFileBase file)
        {
            WebImage img = new WebImage(file.InputStream);
            img.FileName = file.FileName;

            ResizeAndSaveImage(img);
            ResizeAndSaveThumbnail(img);
        }

        public static void SaveFiles(HttpPostedFileBase[] files, ModelStateDictionary modelstate)
        {
            CheckIsFileEntered(files, modelstate);
            
            CheckFileNumberLimitation(files, modelstate);

            ValidateFiles(files, modelstate);
            //if they are all valid then try to save them to disk
            SaveFilesToDisk(files, modelstate);
        }

        public static void DeleteFile(ProductImage productImage, HttpRequestBase request)
        {
            File.Delete(request.MapPath(Constants.ProductImagePath + productImage.FileName));
            File.Delete(request.MapPath(Constants.ProductThumbnailPath + productImage.FileName));
        }

        private static void SaveFilesToDisk(HttpPostedFileBase[] files, ModelStateDictionary modelstate)
        {
            foreach (var file in files)
            {
                try
                {
                    SaveFileToDisk(file);
                }
                catch (Exception)
                {
                    modelstate.AddModelError("FileName",
                        "Sorry an error occurred saving the files to disk, please try again");
                }
            }
        }

        private static void CheckFileNumberLimitation(HttpPostedFileBase[] files, ModelStateDictionary modelstate)
        {
            if (files.Length > 10)
            {
                modelstate.AddModelError("FileName", "Please only upload up to ten files at a time");
            }
        }

        private static void CheckIsFileEntered(HttpPostedFileBase[] files, ModelStateDictionary modelstate)
        {
            //check the user has entered a file
            if (files[0] == null)
            {
                //if the user has not entered a file return an error message
                modelstate.AddModelError("FileName", "Please choose a file");
            }
        }

        private static void ValidateFiles(HttpPostedFileBase[] files, ModelStateDictionary modelstate)
        {
            bool allValid = true;
            string inValidFiles = "";
            foreach (var file in files)
            {
                if (!ValidateFile(file))
                {
                    allValid = false;
                    inValidFiles += ", " + file.FileName;
                }
            }

            if (!allValid)
            {
                modelstate.AddModelError("FileName",
                    "All files must be gif, png, jpeg or jpg and less than 2MB in size.The following files" +
                    inValidFiles + " are not valid");
            }
        }

        private static bool ValidateFile(HttpPostedFileBase file)
        {
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
          
            if (file.ContentLength > 0 && file.ContentLength < Constants.MaxFileSize && Constants.allowedFileTypes.Contains(fileExtension))
            {
                return true;
            }

            return false;
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