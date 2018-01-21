using System;
using System.Xml;

namespace PlexFlux.IPC
{
    public class IPCMessageFactory
    {
        public XmlDocument Create(string action, out XmlNode messageNode)
        {
            var xml = new XmlDocument();
            xml.PreserveWhitespace = true;

            var node = xml.CreateElement("Message");

            var actionAttribute = xml.CreateAttribute("action");
            actionAttribute.Value = action;
            node.Attributes.Append(actionAttribute);

            xml.AppendChild(node);

            messageNode = node;
            return xml;
        }

        public XmlDocument Create(string action)
        {
            XmlNode messageNode;
            return Create(action, out messageNode);
        }
    }
}
