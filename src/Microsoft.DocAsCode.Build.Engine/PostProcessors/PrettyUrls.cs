#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.Build.Engine;

public class PrettyUrls : HtmlDocumentHandler
{
    protected override void HandleCore(HtmlDocument document, ManifestItem manifestItem, string inputFile,
        string outputFile)
    {
        string addToFront = string.Empty;

        string relativePath = manifestItem.OutputFiles[".html"].RelativePath;

        //If this is a root file
        string fileName = Path.GetFileName(relativePath);
        if (!relativePath.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
            addToFront = "../";

        var hyperLinkNodes = document.DocumentNode.SelectNodes($"//a");
        if (hyperLinkNodes != null)
        {
            foreach (HtmlNode node in hyperLinkNodes)
            {
                var href = node.Attributes
                    .FirstOrDefault(x => x.Name.Equals("href", StringComparison.InvariantCultureIgnoreCase));
                if(href == null)
                    continue;

                //Check for html files
                if (href.Value.EndsWith(".html"))
                {
                    href.Value = Path.Combine(addToFront, UpdatePath(href.Value));

                    href.Value = href.Value.Replace("index/index.html", string.Empty);
                    href.Value = href.Value.Replace("index.html", string.Empty);
                }
                else
                {
                    int idTag = href.Value.IndexOf('#');
                    if(idTag <= 0)
                        continue;

                    string tag = href.Value[idTag..];
                    string url = href.Value[..idTag];
                    href.Value = $"{Path.Combine(addToFront, UpdatePath(url)).Replace("index.html", string.Empty)}{tag}";
                }
            }
        }

        var linkNodes = document.DocumentNode.SelectNodes($"//link");
        if (linkNodes != null)
        {
            foreach (HtmlNode linkNode in linkNodes)
            {
                var href = linkNode.Attributes.FirstOrDefault(x =>
                    x.Name.Equals("href", StringComparison.InvariantCultureIgnoreCase));
                if(href == null)
                    continue;

                href.Value = Path.Combine(addToFront, href.Value);
            }
        }

        var scriptNodes = document.DocumentNode.SelectNodes($"//script|//img");
        if (scriptNodes != null)
        {
            foreach (HtmlNode scriptNode in scriptNodes)
            {
                var src = scriptNode.Attributes.FirstOrDefault(x =>
                    x.Name.Equals("src", StringComparison.InvariantCultureIgnoreCase));
                if(src == null)
                    continue;

                src.Value = Path.Combine(addToFront, src.Value);
            }
        }
    }

    protected override Manifest PreHandleCore(Manifest manifest)
    {
        foreach (ManifestItem manifestFile in manifest.Files)
        {
            foreach (KeyValuePair<string,OutputFileInfo> outputFile in manifestFile.OutputFiles)
            {
                if (outputFile.Key.Equals(".html"))
                {
                    Logger.LogVerbose($"Updating {outputFile.Value.RelativePath}...");

                    //This is the main index.html
                    if(outputFile.Value.RelativePath == "index.html" || outputFile.Value.RelativePath.EndsWith("toc.html"))
                        continue;

                    outputFile.Value.RelativePath = UpdatePath(outputFile.Value.RelativePath);
                }
            }
        }

        return manifest;
    }

    private static string UpdatePath(string path)
    {
        string? baseDir = Path.GetDirectoryName(path);

        //Get just the file name
        string? fileName = Path.GetFileNameWithoutExtension(path);

        return Path.Combine(baseDir, fileName, "index.html").ToLower();
    }
}
