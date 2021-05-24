using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
namespace AeroEpub
{
    public class EpubFile
    {
        public string filename;
        public string path;
        public List<EpubFileEntry> entries;
        TextEpubFileEntry _packageFile = null;
        XmlDocument _packageDocument = null;
        public TextEpubFileEntry packageFile
        {
            get
            {
                if (_packageFile == null)
                {
                    TextEpubFileEntry i = GetFile<TextEpubFileEntry>("META-INF/container.xml");
                    XmlDocument container = new XmlDocument();
                    container.LoadXml(i.text);
                    if (i == null) { throw new EpubErrorException("Cannot find META-INF/container.xml"); }
                    var pathNode = container.GetElementsByTagName("rootfile");
                    if (pathNode.Count == 0) throw new EpubErrorException("Cannot valid container.xml");
                    string opf_path = (pathNode[0] as XmlElement).GetAttribute("full-path");
                    _packageFile = GetFile<TextEpubFileEntry>(opf_path);
                    if (_packageFile == null) { throw new EpubErrorException("Cannot find opf file: " + opf_path); }
                }
                return _packageFile;
            }
        }
        XmlDocument packageDocument
        {
            get
            {
                if (_packageDocument == null)
                {
                    _packageDocument = new XmlDocument();
                    _packageDocument.LoadXml(packageFile.text);
                }
                return _packageDocument;
            }
        }

        string idref;
        public MetaRecord uniqueIdentifier;

        public class MetaRecord
        {
            public string name;
            public string value;
            public string id;
            public string tagname = "meta";
            public List<MetaRecord> refines = new List<MetaRecord>();
            public MetaRecord(XmlElement e)
            {
                name = e.Name;
                value = e.InnerText;
                id = e.GetAttribute("id");
                tagname = e.Name;
            }
            public MetaRecord() { }
            public void AddIfExist(XmlElement e, string property_name)
            {
                string t = e.GetAttribute(property_name);
                if (t != "")
                {
                    int pre = property_name.IndexOf(':');
                    if (pre > 0) { property_name = property_name.Substring(pre + 1); }
                    var a = new MetaRecord();
                    a.name = property_name;
                    a.value = t;
                    refines.Add(a);
                }
            }
            public MetaRecord GetRefines(string name)
            {
                foreach (var a in refines) { if (name == a.name) return a; }
                return null;
            }
            public override string ToString()
            {
                StringBuilder s = new StringBuilder();
                s.Append("<");
                s.Append(tagname);
                if (id != "") s.Append(" id=\"" + id + "\"");
                s.Append(">");
                s.Append(value);
                s.Append("</" + tagname + ">");
                return s.ToString();
            }
        }
        string _version;
        public string version
        {
            get { if (_version == null) ReadMeta(); return _version; }
        }
        public string title
        {
            get { if (titleRecords == null) ReadMeta(); return titleRecords[0].value; }
        }
        public string[] creators
        {
            get
            {
                if (creatorRecords == null) ReadMeta();
                string[] creators = new string[creatorRecords.Count];
                for (int i = 0; i < creators.Length; i++) creators[i] = creatorRecords[i].value;
                return creators;
            }
        }
        public string language
        {
            get
            {
                if (languageRecords == null) ReadMeta();
                if (languageRecords.Count > 0)
                    return languageRecords[0].value;
                else
                    return xml_lang;
            }
        }

        public string xml_lang;
        public List<MetaRecord> titleRecords;
        public List<MetaRecord> creatorRecords;
        public List<MetaRecord> languageRecords;
        public List<MetaRecord> identifierRecords;
        public List<MetaRecord> otherRecords;
        public List<MetaRecord> meta;
        public Item coverImage;
        Item _toc;
        public Item toc
        {
            get
            {
                if (_version == null) ReadMeta();
                return _toc;
            }
        }

        public void ReadMeta()
        {
            var packge_tag = packageDocument.GetElementsByTagName("package")[0] as XmlElement;
            idref = packge_tag.GetAttribute("unique-identifier");
            _version = packge_tag.GetAttribute("version");
            xml_lang = packge_tag.GetAttribute("xml:lang");//bookwalker
            titleRecords = new List<MetaRecord>();
            creatorRecords = new List<MetaRecord>();
            languageRecords = new List<MetaRecord>();
            identifierRecords = new List<MetaRecord>();
            otherRecords = new List<MetaRecord>();
            meta = new List<MetaRecord>();
            switch (_version)
            {
                case "3.0": ReadMeta3(); break;
                default: ReadMeta2(); break;
            }
        }
        void ReadMeta2()
        {
            var f = packageDocument.GetElementsByTagName("metadata")[0] as XmlElement;
            foreach (XmlNode node in f.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                var e = (XmlElement)node;
                string n = e.Name;
                switch (n)
                {
                    case "dc:title":
                        {
                            var t = new MetaRecord(e);
                            t.AddIfExist(e, "opf:file-as");
                            titleRecords.Add(t);
                        }
                        break;
                    case "dc:creator":
                        {
                            var t = new MetaRecord(e);
                            t.AddIfExist(e, "opf:file-as");
                            t.AddIfExist(e, "opf:role");
                            creatorRecords.Add(t);
                        }
                        break;
                    case "dc:language":
                        {
                            var t = new MetaRecord(e);
                            languageRecords.Add(t);
                        }
                        break;
                    case "dc:identifier":
                        {
                            var t = new MetaRecord(e);
                            t.AddIfExist(e, "opf:scheme");
                            identifierRecords.Add(t);
                        }
                        break;
                    case "dc:contributor":
                        {
                            var t = new MetaRecord(e);
                            t.AddIfExist(e, "opf:file-as");
                            t.AddIfExist(e, "opf:role");
                            otherRecords.Add(t);
                        }
                        break;
                    case "dc:date":
                        {
                            var t = new MetaRecord(e);
                            t.AddIfExist(e, "opf:event");
                            otherRecords.Add(t);
                        }
                        break;
                    case "meta":
                        {
                            var t = new MetaRecord();
                            t.name = e.GetAttribute("name");
                            t.value = e.GetAttribute("content");
                            meta.Add(t);
                        }
                        break;
                    default:
                        {
                            var t = new MetaRecord(e);
                            otherRecords.Add(t);
                        }
                        break;
                }
            }
            foreach (var a in meta)
            {
                if (a.name == "cover")
                {
                    string id = a.value;
                    if (manifest.ContainsKey(id))
                    {
                        coverImage = manifest[id];
                    }
                    break;
                }
            }
            foreach (var a in identifierRecords)
            {
                if (idref == a.id) { uniqueIdentifier = a; break; }
            }
            _toc = spine.toc;
        }
        void ReadMeta3()
        {
            var f = packageDocument.GetElementsByTagName("metadata")[0] as XmlElement;
            List<MetaRecord> primary = new List<MetaRecord>();
            foreach (XmlNode node in f.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                var e = (XmlElement)node;
                string n = e.Name;
                switch (n)
                {
                    case "dc:language":
                    case "dc:identifier":
                        {
                            var t = new MetaRecord(e);
                            primary.Add(t);
                        }
                        break;
                    case "meta":
                        {
                            string name = e.GetAttribute("name");
                            if (name != "")
                            {
                                var t = new MetaRecord();
                                t.name = name;
                                t.value = e.GetAttribute("content");
                                meta.Add(t);
                                continue;
                            }
                            string refines = e.GetAttribute("refines");
                            if (refines != "")
                            {
                                if (refines.StartsWith("#") && refines.Length > 1)
                                {
                                    string id = refines.Substring(1);
                                    var t = new MetaRecord(e);
                                    t.name = e.GetAttribute("property");
                                    t.AddIfExist(e, "scheme");
                                    foreach (var r in primary)
                                    { //要是refine在primary前面我可不管……
                                        if (r.id == id)
                                        {
                                            r.refines.Add(t);
                                            break;
                                        }
                                    }
                                    continue;
                                }
                            }
                            string property = e.GetAttribute("property");
                            if (property != "")
                            {
                                var t = new MetaRecord(e);
                                t.name = property;
                                meta.Add(t);
                                continue;
                            }
                        }
                        break;
                    default:
                        {
                            var t = new MetaRecord(e);
                            t.AddIfExist(e, "xml:lang");
                            t.AddIfExist(e, "dir");
                            primary.Add(t);
                        }
                        break;
                }
            }
            foreach (var a in primary)
            {
                switch (a.name)
                {
                    case "dc:title": titleRecords.Add(a); break;
                    case "dc:creator": creatorRecords.Add(a); break;
                    case "dc:identifier": identifierRecords.Add(a); break;
                    case "dc:language": languageRecords.Add(a); break;
                    default: otherRecords.Add(a); break;
                }
            }
            foreach (var a in identifierRecords)
            {
                if (idref == a.id) { uniqueIdentifier = a; break; }
            }
            foreach (var a in manifest)
            {
                switch (a.Value.properties)
                {
                    case "nav": _toc = a.Value; break;
                    case "cover-image": coverImage = a.Value; break;
                }
            }
            if (_toc == null) _toc = spine.toc;
            //check
            //if (dc_titles.Count == 0 || dc_identifier.Count == 0 || dc_language.Count == 0) { throw new EpubErrorException("Lack of some metadata."); }
        }
        public void WriteMeta3()
        {
            //2.0 to 3.0 pre-process
            if (coverImage != null)
                coverImage.properties = "cover-image";

            //ToString
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            sb.Append("<package version=\"3.0\" unique-identifier=\"" + idref + "\" xmlns=\"http://www.idpf.org/2007/opf\">\n");
            sb.Append("    <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n");
            WriteMeta3Helper_WriteDcMeta(titleRecords, sb);
            WriteMeta3Helper_WriteDcMeta(creatorRecords, sb);
            WriteMeta3Helper_WriteDcMeta(languageRecords, sb);
            WriteMeta3Helper_WriteDcMeta(identifierRecords, sb);
            WriteMeta3Helper_WriteDcMeta(otherRecords, sb);
            sb.Append("\n");
            sb.Append("    </metadata>\n");

            sb.Append("\n    <manifest>\n");
            foreach (var id_item in manifest)
            {
                var item = id_item.Value;
                sb.Append("        " + item + "\n");
            }
            sb.Append("    </manifest>\n");

            sb.Append("\n    <spine");
            if (spine.pageProgressionDirection != null)
            {
                sb.Append(" page-progression-direction=\"" + spine.pageProgressionDirection + "\"");
            }
            if (spine.toc != null)
            {
                sb.Append(" toc=\"" + spine.toc.id + "\"");
            }
            sb.Append(">\n");
            foreach (var itemref in spine)
            {
                sb.Append("        " + itemref + "\n");
            }
            sb.Append("    </spine>\n");
            sb.Append("</package>");

            packageFile.text = sb.ToString();
            _packageDocument = null;
            ReadMeta();
        }
        private void WriteMeta3Helper_WriteDcMeta(List<MetaRecord> records, StringBuilder sb)
        {
            sb.Append("\n");
            foreach (var record in records)
            {
                //some pre-process, cast OPF2.0
                if (record.refines.Count > 0)
                {
                    if (record.tagname == "dc:date")
                    {
                        //opf:event exists in 2.0, but removed at 3.0 
                        string eve = record.GetRefines("event").value;
                        record.refines.Clear();
                        if (eve == "modification")
                        {
                            record.tagname = "meta";
                            record.name = "dcterms:modified";
                        }
                    }
                    else
                    if (record.id == "")
                    {
                        //dangerous: duplicated id
                        record.id = record.tagname.Replace("dc:", "") + records.IndexOf(record);
                    }

                }

                sb.Append("        ");
                sb.Append(record);
                sb.Append("\n");
                foreach (var refine in record.refines)
                {
                    sb.Append("        ");
                    sb.Append("<meta");
                    sb.Append(" refines=\"#" + record.id + "\"");
                    sb.Append(" property=\"" + refine.name + "\"");
                    if (refine.name == "role")
                        sb.Append(" scheme=\"marc:relators\"");
                    sb.Append(">");
                    sb.Append(refine.value);
                    sb.Append("</meta>");
                    sb.Append("\n");
                }
            }
        }
        Spine _spine;
        Dictionary<string, Item> _manifest;
        public Spine spine
        {
            get
            {
                if (_spine == null) ReadSpine();
                return _spine;
            }
        }
        public Dictionary<string, Item> manifest
        {
            get
            {
                if (_manifest == null) ReadSpine();
                return _manifest;
            }
        }


        void ReadSpine()
        {
            var f = packageDocument.GetElementsByTagName("manifest");
            _manifest = new Dictionary<string, Item>();
            foreach (XmlNode node in f[0].ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                var e = (XmlElement)node;
                if (e.Name != "item") continue;
                var i = new Item(e, this, packageFile.fullName);
                _manifest.Add(i.id, i);
            }
            var f2 = packageDocument.GetElementsByTagName("spine")[0] as XmlElement;
            _spine = new Spine(f2, _manifest);
        }

        public void DeleteEmpty()//只查一层……谁家epub也不会套几个文件夹
        {
            List<EpubFileEntry> tobedelete = new List<EpubFileEntry>();
            foreach (var item in entries)
            {
                if (item.fullName.EndsWith("/"))
                {
                    bool refered = false;
                    foreach (var item2 in entries)
                    {
                        if (item2.fullName != item.fullName && item2.fullName.StartsWith(item.fullName))
                        {
                            refered = true;
                            break;
                        }
                    }
                    if (!refered) tobedelete.Add(item);
                }

            }
            foreach (var a in tobedelete) entries.Remove(a);
        }

        public EpubFileEntry GetFile(string fullName)
        {
            foreach (var i in entries) if (i.fullName == fullName) return i;
            throw new EpubErrorException("Cannot find file by filename:" + fullName);
        }
        public T GetFile<T>(string fullName) where T : EpubFileEntry
        {
            EpubFileEntry r = null;
            foreach (var i in entries) if (i.fullName == fullName) r = i;
            if (r == null || r.GetType() != typeof(T)) return null;
            return (T)r;
        }
        public Item GetItem(string href)
        {

            foreach (var i in manifest) if (i.Value.filePath == href) return i.Value;
            return null;
        }
        public void Save(string path, FileMode fileMode = FileMode.Create)
        {
            string filepath = path;
            if (!path.EndsWith(".epub", StringComparison.OrdinalIgnoreCase))
            {
                filepath = Path.Combine(filepath, filename + ".epub");
            }
            using (FileStream zipToOpen = new FileStream(filepath, fileMode))
            {

                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (var entry in entries) { entry.PutInto(archive); }
                }
            }
            Log.log("[Info]Saved " + filepath);
        }
        const string default_container =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n    <rootfiles>\n        <rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>\n    </rootfiles>\n</container>";
        public EpubFile()
        {
            entries = new List<EpubFileEntry>();
            entries.Add(new EpubMIMETypeEntry());
            entries.Add(new TextEpubFileEntry("META-INF/container.xml", default_container));
            entries.Add(new TextEpubFileEntry("OEBPS/content.opf", ""));
        }
        public EpubFile(EpubFile e)
        {
            //todo
        }
        public EpubFile(string path)
        {
            this.path = path;
            filename = Path.GetFileNameWithoutExtension(path);
            entries = new List<EpubFileEntry>();
            using (FileStream zipToOpen = new FileStream(path, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string ext = Path.GetExtension(entry.Name).ToLower();
                        if (entry.FullName == "mimetype")
                        {
                            using (var stm = entry.Open())
                            using (StreamReader r = new StreamReader(stm))
                            {
                                string s = Util.Trim(r.ReadToEnd());
                                if (s != "application/epub+zip") throw new EpubErrorException("The mimetype of epub should be 'application/epub+zip'. Current:" + s);
                                var i = new EpubMIMETypeEntry();
                                entries.Insert(0, i);
                            }

                        }
                        else
                            switch (ext)
                            {
                                case ".xhtml":
                                case ".html":
                                case ".xml":
                                case ".css":
                                case ".opf":
                                case ".ncx":
                                case ".svg":
                                case ".js":

                                    using (var stm = entry.Open())
                                    using (StreamReader r = new StreamReader(stm))
                                    {
                                        string s = r.ReadToEnd();
                                        var i = new TextEpubFileEntry(entry.FullName, s);
                                        entries.Add(i);
                                    }
                                    break;
                                default:
                                    using (var stm = entry.Open())

                                    {
                                        byte[] d = new byte[entry.Length];
                                        if (entry.Length < int.MaxValue)
                                        {
                                            stm.Read(d, 0, (int)entry.Length);
                                            var i = new EpubFileEntry(entry.FullName, d);
                                            entries.Add(i);
                                        }
                                        else { throw new EpubErrorException("File size exceeds the limit."); }
                                    }
                                    break;
                            }
                    }
                }
            }
            if (entries.Count == 0) throw new EpubErrorException("Cannot find files in epub");
            if (entries[0].GetType() != typeof(EpubMIMETypeEntry)) throw new EpubErrorException("Cannot find mimetype in epub");
        }
    }
    public class Item
    {
        //http://idpf.org/epub/30/spec/epub30-publications.html#sec-item-elem
        public string href, id, mediaType;
        public string properties;
        //public string fallback, mediaOverlay;
        public string filePath;
        EpubFile belongTo;
        public Item(XmlElement e, EpubFile belongTo, string packageFilePath)
        {
            this.belongTo = belongTo;
            href = e.GetAttribute("href");
            id = e.GetAttribute("id");
            mediaType = e.GetAttribute("media-type");
            properties = e.GetAttribute("properties");
            if (href[0] != '/')
            {
                string dir = Path.GetDirectoryName(packageFilePath);
                if (dir != "")
                    filePath = Path.GetDirectoryName(packageFilePath) + "/" + href;
            }
            else { filePath = href; }
        }
        public Item(EpubFile belongTo, EpubFileEntry entry, string id, string mediaType, string properties)
        {
            this.belongTo = belongTo;
            this.id = id;
            this.mediaType = mediaType;
            this.properties = properties;
            string dir = Path.GetDirectoryName(belongTo.packageFile.fullName);
            href = Path.GetRelativePath(dir, entry.fullName).Replace('\\', '/');
        }
        public EpubFileEntry GetFile()
        {
            return belongTo.GetFile(Uri.UnescapeDataString(filePath));
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<item");
            sb.Append(" media-type=\"" + mediaType + "\"");
            sb.Append(" id=\"" + id + "\"");
            sb.Append(" href=\"" + href + "\"");
            if (properties != "")
                sb.Append(" properties=\"" + properties + "\"");
            sb.Append("/>");
            return sb.ToString();
        }
    }
    public class Spine : IEnumerable<Itemref>
    {
        //http://idpf.org/epub/30/spec/epub30-publications.html#sec-spine-elem
        List<Itemref> items = new List<Itemref>();
        public Item toc;//For EPUB2
        public string pageProgressionDirection;
        public string id;
        public Spine(XmlElement spine, Dictionary<string, Item> items)
        {
            string toc = spine.GetAttribute("toc");
            string id = spine.GetAttribute("id");
            if (toc != "")
            {
                this.toc = items[toc];
            }
            pageProgressionDirection = spine.GetAttribute("page-progression-direction");
            foreach (XmlNode node in spine.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                var e = node as XmlElement;
                if (e.Name != "itemref") continue;
                this.items.Add(new Itemref(e, items));
            }
        }
        public int Count { get { return items.Count; } }
        public Itemref this[int index]
        {
            get
            {
                return items[index];
            }
        }

        public IEnumerator<Itemref> GetEnumerator()
        {
            return items.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }


    }
    /// <summary>
    /// An itemref in  Spine that refers to item in Manifest. 
    /// </summary>
    public class Itemref
    {
        //http://idpf.org/epub/30/spec/epub30-publications.html#sec-itemref-elem
        public Item item;
        public string properties;
        public string id;
        public bool linear = true;
        public Itemref(XmlElement itemref, Dictionary<string, Item> items)
        {
            this.item = items[itemref.GetAttribute("idref")];
            properties = itemref.GetAttribute("properties");
            id = itemref.GetAttribute("id");
            if (itemref.GetAttribute("linear") == "no") linear = false;
        }
        public string href { get { return item.href; } }
        public string filePath { get { return item.filePath; } }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<itemref");
            sb.Append(" linear=\"" + (linear ? "yes" : "no") + "\"");
            sb.Append(" idref=\"" + item.id + "\"");
            if (id != "")
                sb.Append(" id=\"" + id + "\"");
            if (properties != "")
                sb.Append(" properties=\"" + properties + "\"");
            sb.Append("/>");
            return sb.ToString();
        }
    }


    public class TextEpubFileEntry : EpubFileEntry
    {
        public string text;

        public TextEpubFileEntry(string fullName, string data)
        {
            this.fullName = fullName;
            this.text = data;

        }
        public override void PutInto(ZipArchive zip)
        {
            var entry = zip.CreateEntry(fullName);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                writer.Write(text);
            }

        }
        public override byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes(text);
        }
    }
    public class EpubFileEntry
    {
        public string fullName;
        byte[] data;
        public EpubFileEntry() { }
        public EpubFileEntry(string fullName, byte[] data)
        {
            this.fullName = fullName;
            this.data = data;

        }
        public virtual void PutInto(ZipArchive zip)
        {
            var entry = zip.CreateEntry(fullName);
            using (Stream stream = entry.Open())
            {
                stream.Write(data, 0, data.Length);
            }
        }
        public virtual byte[] GetBytes()
        {
            return data;
        }
    }
    public class EpubMIMETypeEntry : EpubFileEntry
    {
        public EpubMIMETypeEntry() { fullName = "mimetype"; }
        public override void PutInto(ZipArchive zip)
        {
            var entry = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);

            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                writer.Write("application/epub+zip");
            }

        }
        public override byte[] GetBytes()
        {
            return Encoding.UTF8.GetBytes("mimetype");
        }
    }

    public class EpubErrorException : System.Exception
    {
        public EpubErrorException(string s) : base(s) { }
    }

}