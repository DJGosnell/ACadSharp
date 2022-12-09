using ACadSharp.Entities;
using ACadSharp.Examples.Common;
using ACadSharp.IO;
using ACadSharp.Tables;
using ACadSharp.Tables.Collections;
using System;
using System.Linq;
using System.Numerics;
using CSMath;

namespace ACadSharp.Examples
{
	class Program
	{
		const string file = "../../../../samples/sample_AC1021.dwg";

		static void Main(string[] args)
        {
            using var dwgReader = new DwgReader("../../../../samples/angle.dwg");
            var dwgDoc = dwgReader.Read();

            using var dxfReader = new DxfReader("../../../../samples/angle.dxf");
            var dxfDoc = dxfReader.Read();

            var dxfEntities = dxfDoc.Entities.ToArray();
            var dwgEntities = dwgDoc.Entities.ToArray();


            double AngleBetween(XYZ vector1, XYZ vector2)
            {
                return Math.Atan2(
                    vector1.X * vector2.Y - vector2.X * vector1.Y, 
                    vector1.X * vector2.X + vector1.Y * vector2.Y);
            }

            for (int i = 0; i < dxfEntities.Length; i++)
            {
                if (dxfEntities[i] is Ellipse)
                {
					Console.WriteLine($"DXF Ellipse StartParameter: {((Ellipse)dxfEntities[i]).StartParameter}");
					Console.WriteLine($"DWG Ellipse StartParameter: {((Ellipse)dwgEntities[i]).StartParameter}");
                    Console.WriteLine($"DXF Ellipse StartParameter: {((Ellipse)dxfEntities[i]).EndParameter}");
					Console.WriteLine($"DWG Ellipse StartParameter: {((Ellipse)dwgEntities[i]).EndParameter}");
                }
                else if (dxfEntities[i] is MText)
                {
                    Console.WriteLine($"DXF MText Rotation: {((MText)dxfEntities[i]).Rotation}");
                    Console.WriteLine($"DWG MText Rotation: {((MText)dwgEntities[i]).Rotation}");

                    var dwg = (MText)dwgEntities[i];
                    var dxf = (MText)dxfEntities[i];

                    var angleDwg = AngleBetween(new XYZ(1,0,0), dwg.AlignmentPoint);
                    var angleDxf = AngleBetween(new XYZ(1, 0, 0), dxf.AlignmentPoint);


                    Console.WriteLine($"DWG MText Rotation Calculated: {angleDwg}");
                    Console.WriteLine($"DWG MText Rotation Calculated: {angleDxf}");
                }
                else if (dxfEntities[i] is TextEntity)
                {
                    Console.WriteLine($"DXF TextEntity Rotation: {((TextEntity)dxfEntities[i]).Rotation}");
                    Console.WriteLine($"DXF TextEntity Rotation Converted: {((TextEntity)dxfEntities[i]).Rotation * (Math.PI / 180)}");
                    Console.WriteLine($"DWG TextEntity Rotation: {((TextEntity)dwgEntities[i]).Rotation}");
                }
                else if (dxfEntities[i] is Arc)
                {
                    Console.WriteLine($"DXF Arc StartAngle: {((Arc)dxfEntities[i]).StartAngle}");
                    Console.WriteLine($"DXF Arc StartAngle Converted: {((Arc)dxfEntities[i]).StartAngle * (Math.PI / 180)}");
                    Console.WriteLine($"DWG Arc StartAngle: {((Arc)dwgEntities[i]).StartAngle}"); 
                    Console.WriteLine($"DXF Arc EndAngle: {((Arc)dxfEntities[i]).EndAngle}");
                    Console.WriteLine($"DXF Arc EndAngle Converted: {((Arc)dxfEntities[i]).EndAngle * (Math.PI / 180)}");
                    Console.WriteLine($"DWG Arc EndAngle: {((Arc)dwgEntities[i]).EndAngle}");
                }
            }

            Console.ReadLine();
        }

		/// <summary>
		/// Logs in the console the document information
		/// </summary>
		/// <param name="doc"></param>
		static void ExploreDocument(CadDocument doc)
		{
			Console.WriteLine();
			Console.WriteLine("SUMMARY INFO:");
			Console.WriteLine($"\tTitle: {doc.SummaryInfo.Title}");
			Console.WriteLine($"\tSubject: {doc.SummaryInfo.Subject}");
			Console.WriteLine($"\tAuthor: {doc.SummaryInfo.Author}");
			Console.WriteLine($"\tKeywords: {doc.SummaryInfo.Keywords}");
			Console.WriteLine($"\tComments: {doc.SummaryInfo.Comments}");
			Console.WriteLine($"\tLastSavedBy: {doc.SummaryInfo.LastSavedBy}");
			Console.WriteLine($"\tRevisionNumber: {doc.SummaryInfo.RevisionNumber}");
			Console.WriteLine($"\tHyperlinkBase: {doc.SummaryInfo.HyperlinkBase}");
			Console.WriteLine($"\tCreatedDate: {doc.SummaryInfo.CreatedDate}");
			Console.WriteLine($"\tModifiedDate: {doc.SummaryInfo.ModifiedDate}");

			ExploreTable(doc.AppIds);
			ExploreTable(doc.BlockRecords);
			ExploreTable(doc.DimensionStyles);
			ExploreTable(doc.Layers);
			ExploreTable(doc.LineTypes);
			ExploreTable(doc.TextStyles);
			ExploreTable(doc.UCSs);
			ExploreTable(doc.Views);
			ExploreTable(doc.VPorts);
		}

		static void ExploreTable<T>(Table<T> table)
			where T : TableEntry
		{
			Console.WriteLine($"{table.ObjectName}");
			foreach (var item in table)
			{
				Console.WriteLine($"\tName: {item.Name}");

				if (item.Name == BlockRecord.ModelSpaceName && item is BlockRecord model)
				{
					Console.WriteLine($"\t\tEntities in the model:");
					foreach (var e in model.Entities.GroupBy(i => i.GetType().FullName))
					{
						Console.WriteLine($"\t\t{e.Key}: {e.Count()}");
					}
				}
			}
		}
	}
}
