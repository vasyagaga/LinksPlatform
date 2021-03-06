﻿using System.Collections.Generic;
using System.IO;
using System.Xml;
using Platform.Converters;
using Platform.Communication.Protocol.Gexf;
using Platform.Data;
using Platform.Data.Doublets;
using Node = Platform.Communication.Protocol.Gexf.Node;

namespace Platform.Examples
{
    public class GEXFExporter<TLink>
    {
        private static readonly UncheckedConverter<TLink, long> _addressToInt64Converter = UncheckedConverter<TLink, long>.Default;

        private readonly ILinks<TLink> _links;

        public GEXFExporter(ILinks<TLink> links)
        {
            _links = links;
        }

        public void Export(string outputFilename)
        {
            using (var file = File.OpenWrite(outputFilename))
            using (var writer = XmlWriter.Create(file))
            {
                // <?xml version="1.0" encoding="UTF-8"?>
                writer.WriteStartDocument();
                // <gexf xmlns="http://www.gexf.net/1.2draft" version="1.2">
                writer.WriteStartElement("gexf", "http://www.gexf.net/1.2draft");
                writer.WriteAttributeString("version", "1.2");
                // <graph mode="static" defaultedgetype="directed">
                writer.WriteStartElement("graph");
                writer.WriteAttributeString("mode", "static");
                writer.WriteAttributeString("defaultedgetype", "directed");
                // <nodes>
                writer.WriteStartElement("nodes");
                _links.Each(link =>
                {
                    Node.WriteXml(writer, _addressToInt64Converter.Convert(link[_links.Constants.IndexPart]), FormatLink(link));
                    return _links.Constants.Continue;
                });
                // </nodes>
                writer.WriteEndElement();
                ulong edges = 0;
                // <edges>
                writer.WriteStartElement("edges");
                _links.Each(link =>
                {
                    Edge.WriteXml(writer, (long)edges++, _addressToInt64Converter.Convert(link[_links.Constants.SourcePart]), _addressToInt64Converter.Convert(link[_links.Constants.TargetPart]));
                    return _links.Constants.Continue;
                });
                // </edges>
                writer.WriteEndElement();
                // </graph>
                writer.WriteEndElement();
                // </gexf>
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private string FormatLink(IList<TLink> link)
        {
            const string format = "{1} {0} {2}"; // "{0}: {1} -> {2}"
            return string.Format(format, link[_links.Constants.IndexPart], link[_links.Constants.SourcePart], link[_links.Constants.TargetPart]);
        }
    }
}