using System.Drawing;
using Inventor;
using System.Windows.Forms;
#pragma warning disable CA1416 // Validate platform compatibility
namespace Inventor82
{
    public class PictureDispConverter : AxHost
    {
        private PictureDispConverter() : base("") { }
        public static IPictureDisp ToIPictureDisp(Image image)
        {
            return (IPictureDisp)GetIPictureDispFromPicture(image);
        }
    }
}
