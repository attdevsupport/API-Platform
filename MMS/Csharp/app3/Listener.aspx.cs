using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;


/* This function is called when application is getting loaded, which reads the MO data and parse the data and saves image to the MoImages directory */
public partial class Listener : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Random random = new Random();

        string mmsFilePath = Request.MapPath(@"MoImages\");

        string inputStreamContents;

        int stringLength;
        int strRead;

        System.IO.Stream str = Request.InputStream;


        stringLength = Convert.ToInt32(str.Length);

        byte[] stringArray = new byte[stringLength];

        strRead = str.Read(stringArray, 0, stringLength);

        inputStreamContents = System.Text.Encoding.UTF8.GetString(stringArray);

        string[] splitData = Regex.Split(inputStreamContents, "</sender-address>");
        string data = splitData[0].ToString();

        String senderAddress = inputStreamContents.Substring(data.IndexOf("tel:") + 4, data.Length - (data.IndexOf("tel:") + 4));

        String[] parts = Regex.Split(inputStreamContents, "--Nokia-mm-messageHandler-BoUnDaRy");
        String[] lowerParts = Regex.Split(parts[2], "BASE64");
        String[] imageType = Regex.Split(lowerParts[0], "image/");
        int indexOfSemicolon = imageType[1].IndexOf(";");

        string type = imageType[1].Substring(0, indexOfSemicolon);

        System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
        System.Text.Decoder utf8Decode = encoder.GetDecoder();

        byte[] todecode_byte = Convert.FromBase64String(lowerParts[1]);
        FileStream fs = new FileStream(mmsFilePath + random.Next() + "." + type, FileMode.CreateNew, FileAccess.Write);
        fs.Write(todecode_byte, 0, todecode_byte.Length);
        fs.Close();
    }
}