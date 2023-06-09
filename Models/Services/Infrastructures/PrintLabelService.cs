using System.Drawing;
using System.Drawing.Printing;

namespace mes.Models.Services.Infrastructures
{
    public class PrintLabelService
    {

        //"PageWidth": 288.0,
        //"PageHeight": 144.0,
        //"PrinterName": "ZebraZT220",
        //"Author": "",
        //"Comment": "Lago-Diretta-ZPL"

        //printResult = PrintPDF(destPrinter, "", file2print, 1);
        //file2print è un nome file locale

        public bool PrintBitmap(Bitmap bitmap, int dpi, int fontSize1, int fontSize2, int lineSpacing, 
                                string cliente, string dataIngresso, string printerName, int dimX, int dimY, int copie, string spessore)
        {
            Bitmap first = new Bitmap (bitmap);
            first.SetResolution(dpi, dpi);

            Bitmap fusion = new Bitmap(dimX, dimY);
            fusion.SetResolution(dpi, dpi);

            Graphics g = Graphics.FromImage(fusion);
            g.DrawImageUnscaled(first, 0, 0);

            Font arialFont1 = new Font("Arial", fontSize1);
            Font arialFont2 = new Font("Arial", fontSize2);
            Font arialFont3 = new Font("Arial", fontSize2 + 20);

            g.DrawString("Cliente", arialFont1, Brushes.Black, new Point(first.Width, 10));
            g.DrawString(cliente, arialFont2, Brushes.Black, new Point(first.Width, 80));
            g.DrawString(spessore, arialFont2, Brushes.Black, new Point(first.Width+500, 30));
            g.DrawString("data di ingresso", arialFont1, Brushes.Black, new Point(first.Width, 250));
            g.DrawString(dataIngresso, arialFont2, Brushes.Black, new Point(first.Width, 350));

            PrintDocument doc = new PrintDocument();
        
            doc.PrinterSettings.PrinterName = printerName;
            doc.PrinterSettings.Copies = (short)copie;

            if (!doc.PrinterSettings.IsValid)
            {
                return false;
            }

            doc.PrintPage += (s, ev) => {
                ev.Graphics.DrawImage(fusion, Point.Empty); // adjust this to put the image elsewhere
                ev.HasMorePages = false;
            };
            doc.Print();

            return true;
        }
      
    }
}