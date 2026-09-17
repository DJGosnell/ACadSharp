using ACadSharp.Entities;
using System.Collections.Generic;

namespace ACadSharp.IO.Templates
{
	internal class CadPolyLineTemplate : CadEntityTemplate, ICadOwnerTemplate
	{
		public ulong? FirstVertexHandle { get; internal set; }

		public ulong? LastVertexHandle { get; internal set; }

		public ulong? SeqendHandle { get; internal set; }

		public HashSet<ulong> OwnedObjectsHandlers { get; } = new();

		public CadPolyLineTemplate() : base(new PolyLinePlaceholder())
		{
		}

		public CadPolyLineTemplate(IPolyline entity) : base((Entity)entity)
		{
		}

		public void SetPolyLineObject<T>(Polyline<T> polyLine)
			where T : Entity, IVertex
		{
			//Already the right object: replace it and everything read into it so far is orphaned. A
			//file that repeats a subclass marker runs this twice, and without the guard it would
			//keep only the six properties copied below, losing the flags and the elevation read
			//between the two. Same guard, and the same reason, as
			//CadDimensionTemplate.SetDimensionObject.
			//It covers only a marker naming the type already held. A marker naming a *different*
			//type must replace the object, which is why no reader may keep a reference to the one
			//it passed in - see DxfSectionReaderBase.readLegacyPolyline.
			if (this.CadObject.GetType() == polyLine.GetType())
			{
				return;
			}

			polyLine.Handle = this.CadObject.Handle;
			polyLine.Color = this.CadObject.Color;
			polyLine.LineWeight = this.CadObject.LineWeight;
			polyLine.LineTypeScale = this.CadObject.LineTypeScale;
			polyLine.IsInvisible = this.CadObject.IsInvisible;
			polyLine.Transparency = this.CadObject.Transparency;

			this.CadObject = polyLine;
		}

		/// <summary>
		/// Adds vertices to whatever polyline this template currently holds.
		/// </summary>
		/// <remarks>
		/// Public rather than protected because the pre-R13 reader collects handle-less vertices
		/// during the read instead of through <see cref="build"/>, and it must reach the object the
		/// record declared rather than the one it seeded. This is the single place that knows how to
		/// put a vertex into each polyline family, and there is no second copy of that switch.
		/// </remarks>
		public void AddVertices(CadDocumentBuilder builder, params IEnumerable<Entity> vertices)
		{
			switch (this.CadObject)
			{
				case Polyline2D pline2d:
					this.addVerticesOfType(builder, pline2d.Vertices, vertices);
					break;
				case Polyline3D pline3d:
					this.addVerticesOfType(builder, pline3d.Vertices, vertices);
					break;
				case PolyfaceMesh mesh:
					foreach (var item in vertices)
					{
						this.addPolyfaceMeshVertex(builder, mesh, item);
					}
					break;
				case PolygonMesh polygon:
					this.addVerticesOfType(builder, polygon.Vertices, vertices);
					break;
				default:
					builder.Notify($"Unknown polyline type {this.CadObject.SubclassMarker}", NotificationType.Warning);
					break;
			}
		}

		/// <summary>
		/// Adds the vertices this collection can hold and reports the rest.
		/// </summary>
		/// <remarks>
		/// A record may name a vertex subclass that contradicts the one its POLYLINE named - a
		/// VERTEX marked <c>AcDb2dVertex</c> inside a POLYLINE marked <c>AcDb3dPolyline</c>. The
		/// collection was reached by a <c>Cast&lt;T&gt;</c>, which throws <c>InvalidCastException</c>
		/// on the first such vertex; and this runs from <see cref="build"/>, inside
		/// <c>CadDocumentBuilder</c>'s template loop, which has no failsafe of its own. So one
		/// contradictory record cost the whole document rather than itself, on both readers.
		/// Reporting and dropping what does not fit is what every other arm of this class already
		/// does - this method's own <c>default:</c>, <see cref="SetSeqend"/>'s,
		/// <see cref="addPolyfaceMeshVertex"/>'s final <c>else</c>, and <see cref="build"/>'s
		/// "not found".
		/// </remarks>
		private void addVerticesOfType<T>(CadDocumentBuilder builder, SeqendCollection<T> collection, IEnumerable<Entity> vertices)
			where T : CadObject
		{
			foreach (Entity vertex in vertices)
			{
				if (vertex is T fits)
				{
					collection.Add(fits);
				}
				else
				{
					builder.Notify(
						$"Vertex {vertex.Handle} is a {vertex.GetType().Name}, which a {this.CadObject.GetType().Name} cannot hold; it is not added to polyline {this.CadObject.Handle}",
						NotificationType.Warning);
				}
			}
		}

		protected override void build(CadDocumentBuilder builder)
		{
			base.build(builder);

			IPolyline polyLine = this.CadObject as IPolyline;

			if (builder.TryGetCadObject<Seqend>(this.SeqendHandle, out Seqend seqend))
			{
				this.SetSeqend(builder, seqend);
			}

			if (this.FirstVertexHandle.HasValue)
			{
				IEnumerable<Vertex> vertices = this.getEntitiesCollection<Vertex>(builder, this.FirstVertexHandle.Value, this.LastVertexHandle.Value);
				this.AddVertices(builder, vertices);
			}
			else
			{
				if (this.CadObject is PolyfaceMesh mesh)
				{
					this.buildPolyfaceMesh(mesh, builder);
				}
				else
				{
					foreach (var handle in this.OwnedObjectsHandlers)
					{
						if (builder.TryGetCadObject(handle, out Vertex v))
						{
							this.AddVertices(builder, v);
						}
						else if (builder.TryGetCadObject(handle, out Seqend s))
						{
							this.SetSeqend(builder, s);
						}
						else
						{
							builder.Notify($"Vertex {handle} not found for polyline {this.CadObject.Handle}", NotificationType.Warning);
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets the seqend on whatever polyline this template currently holds.
		/// </summary>
		/// <remarks>Public for the same reason as <see cref="AddVertices"/>.</remarks>
		public void SetSeqend(CadDocumentBuilder builder, Seqend seqend)
		{
			switch (this.CadObject)
			{
				case Polyline2D pline2d:
					pline2d.Vertices.Seqend = seqend;
					break;
				case Polyline3D pline3d:
					pline3d.Vertices.Seqend = seqend;
					break;
				case PolyfaceMesh mesh:
					mesh.Vertices.Seqend = seqend;
					break;
				case PolygonMesh polygon:
					polygon.Vertices.Seqend = seqend;
					break;
				default:
					builder.Notify($"Unknown polyline type {this.CadObject.SubclassMarker}", NotificationType.Warning);
					break;
			}
		}

		private void buildPolyfaceMesh(PolyfaceMesh polyfaceMesh, CadDocumentBuilder builder)
		{
			foreach (var handle in this.OwnedObjectsHandlers)
			{
				if (builder.TryGetCadObject(handle, out Entity e))
				{
					this.addPolyfaceMeshVertex(builder, polyfaceMesh, e);
				}
			}
		}

		private void addPolyfaceMeshVertex(CadDocumentBuilder builder, PolyfaceMesh polyfaceMesh, Entity e)
		{
			if (e is VertexFaceMesh v3)
			{
				polyfaceMesh.Vertices.Add(v3);
			}
			else if (e is VertexFaceRecord face)
			{
				polyfaceMesh.Faces.Add(face);
			}
			else if (e is Seqend seqend)
			{
				polyfaceMesh.Vertices.Seqend = seqend;
			}
			else
			{
				builder.Notify($"Unidentified type for PolyfaceMesh {e.GetType().FullName}");
			}
		}

		internal class PolyLinePlaceholder : Polyline<Vertex>
		{
			public override ObjectType ObjectType { get { return ObjectType.INVALID; } }
		}
	}
}