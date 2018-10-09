namespace YAPC
{
	public class FileToDeleteModel
	{
		public string Name { get; set; }
		public string Location { get; set; }
		public string FullPath { get; set; }
		public long SizeInBytes { get; set; }
		public string ProductCode { get; set; }
		public string PatchCode { get; set; }
		public string ProductName { get; set; }
		public string ProductVersion { get; set; }
		public string ProductPublisher { get; set; }
		public string PatchName { get; set; }
	}
}