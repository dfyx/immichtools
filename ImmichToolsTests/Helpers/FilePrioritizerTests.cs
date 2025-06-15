using ImmichTools.Helpers;
using ImmichTools.ReplyData;

namespace ImmichToolsTests.Helpers;

[TestClass]
public class FilePrioritizerTests
{
    [TestMethod]
    [DataRow("IMG_1234.CR2", -99)]
    [DataRow("IMG_1234.cr2", -99)]
    [DataRow("IMG_1234.dng", -99)]
    [DataRow("IMG_1234-Enhanced-NR.dng", -1)]
    [DataRow("IMG_1234.jpg", 0)]
    [DataRow("IMG_1234.cr23", 0)]
    [DataRow("IMG_1234.psd", 1)]
    public void TestFilePriorities(string fileName, int expectedPriority)
    {
        var asset = new Asset
        {
            Id = string.Empty,
            OriginalPath = fileName,
            OriginalFileName = fileName,
            LocalDateTime = DateTime.MinValue
        };

        Assert.AreEqual(expectedPriority, FilePrioritizer.GetFilePriority(asset));
    }
}