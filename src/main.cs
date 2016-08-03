using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public static class Directories{
	public static string sourceDir { get; set; }
	public static string destDir { get; set; }
}

public class PhotoSorter {
	static void Main(string[] args) {
		
		string[] files;
		// Extensions supported by the Bitmap class
		string[] ext = {".bmp", ".gif", ".exif", ".jpg", ".png", ".tiff"};

		if(args.Length < 2){
			Console.WriteLine("Usage: photosorter <source_directory> <destination_directory>");
			return;
		}
		
		// Get rid of quotes if they were used to contain directories, trim and readd slashes so directories are made correctly
		string path = Directories.sourceDir = args[0].Trim('\"').TrimEnd('/').TrimEnd('\\');
		Directories.destDir = args[1].Trim('\"').TrimEnd('/').TrimEnd('\\') + "/";
		
		if(Directory.Exists(path)) {
			try{
				files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Where(file => ext.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToArray();
			}
			catch (Exception e) {
				Console.WriteLine(e.Message);
				return;
			}
			
			Console.WriteLine("Copying " + files.Length + " files from " + path);
			
			int errors = ProcessDirectory(files);
			
			if(errors > 0){
				Console.WriteLine(errors + " errors copying from " + path);
			}
		
			if (files.Length == 0) {
				Console.WriteLine("No files found in " + path);
			}
		}
		else if(File.Exists(path)) {
			Console.WriteLine(path + " is a file, not a directory.");
		}
		else {
			Console.WriteLine(path + " returned no results.");
		}
	}
	
	public static int ProcessDirectory(string[] files) {
		int count = files.Length;
		int progress = 0;
		int percentage = 0;
		int errorCount = 0;
		
		string workingDir = Directories.destDir;
		
		Console.WriteLine("0%");
		
		foreach(string path in files) {
			
			Image image = new Bitmap(path);
			
			PropertyItem[] propItems = image.PropertyItems;
			
			string dateTaken = FindDateTaken(propItems).Split(' ').First().Replace(':', '_');
			
			// Until I think of a better way to do this, no EXIF means revert to file modified time. This seems like the most accurate alternative
			if(dateTaken == "0"){
				FileInfo fi = new FileInfo(path);
				dateTaken = fi.LastWriteTime.Year + "_" + fi.LastWriteTime.Month + "_" + fi.LastWriteTime.Day;
			}
			
			Directory.CreateDirectory(workingDir + dateTaken);
			
			// Basic progress output, displays every 1%
			++progress;
			if(progress * 100 / count > percentage){
				++percentage;
				Console.WriteLine(percentage + "%");
			}
			
			bool copySuccess = false;
			int copyRetry = 0;
			while(!copySuccess) {
				try {
					string copyPath = workingDir + dateTaken + @"\" + path.Split('\\').Last();
					
					// Check if a file already exists with this name
					while(File.Exists(copyPath))
					{
						++copyRetry;
						
						// If it does, try adding a number to the end
						copyPath = workingDir + dateTaken + @"\" + path.Split('\\').Last();
						copyPath = copyPath.Insert(copyPath.LastIndexOf('.'), " (" + copyRetry + ")");
						
						// Give up after trying 1000 numbers
						if(copyRetry > 1000){
							throw new IOException("Could not find valid name for duplicate file.");
						}
					}
					
					File.Copy(path, copyPath);
					copySuccess = true;
					break;
				} 
				catch (IOException e) {
					Console.WriteLine(path + "\nCould not copy file. " + e.Message);
					++errorCount;
					break;
				}
			}
		}
		
		return errorCount;
	}
	
	public static string FindDateTaken(PropertyItem[] propItems) {
		foreach (PropertyItem propItem in propItems) { // Check EXIF DateTimeOriginal data
			if(Int32.Parse(propItem.Id.ToString()) == 36867 && !String.IsNullOrEmpty(System.Text.Encoding.UTF8.GetString(propItem.Value).TrimEnd('\0'))) {
				return System.Text.Encoding.UTF8.GetString(propItem.Value).TrimEnd('\0');
			}
		}
		
		foreach (PropertyItem propItem in propItems) { // Check EXIF DateTimeDigitized data
			if(Int32.Parse(propItem.Id.ToString()) == 36868 && !String.IsNullOrEmpty(System.Text.Encoding.UTF8.GetString(propItem.Value).TrimEnd('\0'))) { 
				return System.Text.Encoding.UTF8.GetString(propItem.Value).TrimEnd('\0');
			}
		}
		
		// Fallback to zero. We'll use the last modified date
		return "0";
	}
}