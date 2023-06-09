using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.pdf;
using mes.Models.ViewModels;

namespace mes.Models.Services.Infrastructures
{
    public class PdfGenerator
    {
        public byte[] GeneratePdf<T>(IEnumerable<T> data)
        {
            
            // Create a new PDF document
            Document document = new Document();

            // Create a new MemoryStream to write the PDF content to
            MemoryStream stream = new MemoryStream();

            // Create a PdfWriter to write the PDF document to the MemoryStream
            PdfWriter writer = PdfWriter.GetInstance(document, stream);

            // Open the document
            document.Open();

            // Create a new PdfPTable to hold the data
            PdfPTable table = new PdfPTable(typeof(T).GetProperties().Length);
            table.WidthPercentage = 100;

            // Add the table headers
            foreach (PropertyInfo property in typeof(T).GetProperties())
            {
                table.AddCell(new PdfPCell(new Phrase(property.Name)));
            }

            // Loop through the data and add it to the table
            foreach (T item in data)
            {
                foreach (PropertyInfo property in typeof(T).GetProperties())
                {
                    table.AddCell(new PdfPCell(new Phrase(property.GetValue(item, null)?.ToString() ?? "")));
                }
            }

            // Add the table to the document
            document.Add(table);

            // Set the position of the MemoryStream to 0
            //stream.Position = 0;

                        // Close the document and writer
            document.Close();
            writer.Close();

            // Return the PDF file as a FileResult to prompt the user to download it
            //return File(stream, "application/pdf", "Table.pdf");

            byte[] bites = stream.ToArray();

            File.WriteAllBytes(@"c:\temp\test.pdf", bites);

            return bites;
        }

        public byte[] GeneratePermessiPdf(List<PermessoViewModel> data)
        {
            Document document = new Document();
            MemoryStream stream = new MemoryStream();
            PdfWriter writer = PdfWriter.GetInstance(document, stream);

            document.Open();

            PdfPTable table = new PdfPTable(9);
            table.WidthPercentage = 100;
//
            // Add the table headers
            table.AddCell("nome");
            table.AddCell("cognome");
            table.AddCell("data inizio");
            table.AddCell("data fine");
            table.AddCell("richiesto il");
            table.AddCell("tipologia");
            table.AddCell("durata");
            table.AddCell("motivazione");
            table.AddCell("urgente");

//
            // Loop through the data and add it to the table
            foreach (PermessoViewModel item in data)
            {
                table.AddCell(new PdfPCell(new Phrase(item.Nome)));
                table.AddCell(new PdfPCell(new Phrase(item.Cognome)));
                table.AddCell(new PdfPCell(new Phrase(item.DataInizio)));
                table.AddCell(new PdfPCell(new Phrase(item.DataFine)));
                table.AddCell(new PdfPCell(new Phrase(item.DataDiRichiesta)));
                table.AddCell(new PdfPCell(new Phrase(item.Motivazione)));
                table.AddCell(new PdfPCell(new Phrase(item.Urgente)));
            }

            // Add the table to the document
            document.Add(table);

            // Close the document and writer
            document.Close();
            writer.Close();

            byte[] bites = stream.ToArray();

            File.WriteAllBytes(@"c:\temp\test.pdf", bites);

            return bites;            
        }

    }
}