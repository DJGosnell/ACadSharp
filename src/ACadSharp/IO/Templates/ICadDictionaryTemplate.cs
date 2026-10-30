namespace ACadSharp.IO.Templates
{
	internal interface ICadDictionaryTemplate : ICadObjectTemplate
	{
		// Redeclares ICadObjectTemplate.CadObject with a setter; both bind to the same property on CadDictionaryTemplate.
		public new CadObject CadObject { get; set; }
	}
}
