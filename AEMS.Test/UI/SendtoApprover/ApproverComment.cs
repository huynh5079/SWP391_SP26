using System;
using System.IO;
using System.Text.Json;

namespace AEMS.Test.UI.SendtoApprover
{
	public class ApproverComment
	{
		public string Comment { get; set; }

		public static ApproverComment[] LoadComments()
		{
			var path = Path.Combine("TestData", "Approvercomment", "comment.json");

			var json = File.ReadAllText(path);

			return JsonSerializer.Deserialize<ApproverComment[]>(json);
		}
	}
}