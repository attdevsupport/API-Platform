using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

public partial class Listener : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
	  //System.IO.File.WriteAllText(@"C:\Users\Public\TestFolder\WriteText.txt", text);
        Random random = new Random();
        DateTime currentServerTime = DateTime.UtcNow;
	  string receivedTime = currentServerTime.ToString("HH-MM-SS");
        string receivedDate = currentServerTime.ToString("MM-dd-yyyy");
        string inputStreamContents;
        int stringLength;
        int strRead;
        System.IO.Stream str = Request.InputStream;
        stringLength = Convert.ToInt32(str.Length);
        byte[] stringArray = new byte[stringLength];
        strRead = str.Read(stringArray, 0, stringLength);
        inputStreamContents = System.Text.Encoding.UTF8.GetString(stringArray);
        string[] splitData = Regex.Split(inputStreamContents, "</SenderAddress>");
        string data=splitData[0].ToString();
        String senderAddress = inputStreamContents.Substring(data.IndexOf("tel:") + 4, data.Length - (data.IndexOf("tel:") + 4));
        String[] parts = Regex.Split(inputStreamContents,"--Nokia-mm-messageHandler-BoUnDaRy");
        String[] lowerParts = Regex.Split(parts[2],"BASE64");
        String[] imageType = Regex.Split(lowerParts[0], "image/");
        int indexOfSemicolon=imageType[1].IndexOf(";");
        string type = imageType[1].Substring(0,indexOfSemicolon);
        System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
        System.Text.Decoder utf8Decode = encoder.GetDecoder();
        byte[] todecode_byte = Convert.FromBase64String(lowerParts[1]);
        //Give Images directory as path example: @"D:\folder"
        if (Directory.Exists(@"D:\Webs\wincod\APIPlatform\2\0\1\PROD\Csharp-RESTful\mms\app3\MoImages\"))
        {
        }
        else
        {
            //Give Images directory as path example: @"D:\folder", same as above value
            System.IO.Directory.CreateDirectory(@"D:\Webs\wincod\APIPlatform\2\0\1\PROD\Csharp-RESTful\mms\app3\MoImages\");
        }
        string fileNameToSave = "From_" + senderAddress + "_At_" + receivedTime + "_UTC_On_" +  receivedDate + random.Next() ;
        //Give Images directory as first argument example: @"D:\folder", same as above value
        FileStream fs = new FileStream(@"D:\Webs\wincod\APIPlatform\2\0\1\PROD\Csharp-RESTful\mms\app3\MoImages\" + fileNameToSave + "." + type, FileMode.CreateNew, FileAccess.Write);
        fs.Write(todecode_byte, 0, todecode_byte.Length);
        fs.Close();
//random.Next() 
    }
}