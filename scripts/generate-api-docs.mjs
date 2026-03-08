import { execFileSync } from "node:child_process";
import { mkdirSync, readdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..");
const docsRoot = path.join(repoRoot, "docs");
const apiRoot = path.join(docsRoot, "api");
const generatedRoot = path.join(apiRoot, "reference");
const sidebarFile = path.join(apiRoot, "sidebar.mjs");
const projectFile = path.join(
  repoRoot,
  "src",
  "RoyalApps.Community.ExternalApps.WinForms",
  "RoyalApps.Community.ExternalApps.WinForms.csproj");
const xmlDocFile = path.join(
  repoRoot,
  "src",
  "RoyalApps.Community.ExternalApps.WinForms",
  "bin",
  "Release",
  "net10.0-windows",
  "RoyalApps.Community.ExternalApps.WinForms.xml");
const documentedNamespacePrefix = "RoyalApps.Community.ExternalApps.WinForms";
const publicApi = collectPublicApiSurface(
  path.join(repoRoot, "src", "RoyalApps.Community.ExternalApps.WinForms"));

execFileSync(
  "dotnet",
  ["build", projectFile, "-c", "Release", "-nologo"],
  { cwd: repoRoot, stdio: "inherit" });

mkdirSync(generatedRoot, { recursive: true });
for (const entry of readdirSync(generatedRoot, { withFileTypes: true })) {
  rmSync(path.join(generatedRoot, entry.name), { recursive: true, force: true });
}
for (const entry of readdirSync(apiRoot, { withFileTypes: true })) {
  if (!entry.isFile()) {
    continue;
  }

  if (entry.name === "index.md" || entry.name === "sidebar.mjs") {
    continue;
  }

  if (entry.name === ".manifest" || entry.name === "toc.yml" || entry.name.endsWith(".yml")) {
    rmSync(path.join(apiRoot, entry.name), { force: true });
  }
}

const xml = readFileSync(xmlDocFile, "utf8");
const members = Array.from(xml.matchAll(/<member name="([^"]+)">([\s\S]*?)<\/member>/g)).map(match => ({
  id: match[1],
  body: match[2]
}));

const memberDocs = new Map(members.map(member => [member.id, parseMemberBody(member.body)]));
const typeMembers = members.filter(member =>
  member.id.startsWith("T:") &&
  publicApi.publicTypes.has(member.id.slice(2)));
const pagesByNamespace = new Map();

for (const typeMember of typeMembers) {
  const fullTypeName = typeMember.id.slice(2);
  const namespaceName = fullTypeName.slice(0, fullTypeName.lastIndexOf("."));
  const typeName = fullTypeName.slice(fullTypeName.lastIndexOf(".") + 1);
  const simpleTypeName = typeName.split("`")[0];
  const slug = slugify(fullTypeName);
  const linkedMembers = members
    .filter(member =>
      member.id !== typeMember.id &&
      getOwningType(member.id) === fullTypeName &&
      isPublicMember(fullTypeName, member.id))
    .sort(compareMembers);

  const pagePath = path.join(generatedRoot, `${slug}.md`);
  writeFileSync(pagePath, renderTypePage(fullTypeName, simpleTypeName, typeMember.id, linkedMembers, memberDocs));

  const entries = pagesByNamespace.get(namespaceName) ?? [];
  entries.push({ fullTypeName, simpleTypeName, slug, summary: memberDocs.get(typeMember.id)?.summary ?? "" });
  pagesByNamespace.set(namespaceName, entries);
}

for (const entries of pagesByNamespace.values()) {
  entries.sort((left, right) => left.simpleTypeName.localeCompare(right.simpleTypeName));
}

const orderedNamespaces = Array.from(pagesByNamespace.keys()).sort((left, right) => left.localeCompare(right));
writeFileSync(path.join(apiRoot, "index.md"), renderApiIndex(orderedNamespaces, pagesByNamespace));
writeFileSync(sidebarFile, renderSidebarModule(orderedNamespaces, pagesByNamespace));

function parseMemberBody(body) {
  return {
    summary: cleanupXml(extractTag(body, "summary")),
    remarks: cleanupXml(extractTag(body, "remarks")),
    returns: cleanupXml(extractTag(body, "returns")),
    params: Array.from(body.matchAll(/<param name="([^"]+)">([\s\S]*?)<\/param>/g)).map(match => ({
      name: match[1],
      description: cleanupXml(match[2])
    }))
  };
}

function extractTag(body, tagName) {
  const match = body.match(new RegExp(`<${tagName}>([\\s\\S]*?)<\\/${tagName}>`));
  return match ? match[1] : "";
}

function cleanupXml(text) {
  return text
    .replace(/<see\s+langword="([^"]+)"\s*\/>/g, "`$1`")
    .replace(/<paramref\s+name="([^"]+)"\s*\/>/g, "`$1`")
    .replace(/<see\s+cref="([^"]+)"\s*\/>/g, (_, cref) => `\`${formatCref(cref)}\``)
    .replace(/<c>([\s\S]*?)<\/c>/g, "`$1`")
    .replace(/<\/?para>/g, "\n\n")
    .replace(/<\/?[^>]+>/g, "")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, "\"")
    .replace(/&apos;/g, "'")
    .replace(/&amp;/g, "&")
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter((line, index, lines) => line.length > 0 || (index > 0 && lines[index - 1].length > 0))
    .join("\n");
}

function formatCref(cref) {
  const value = cref.includes(":") ? cref.slice(cref.indexOf(":") + 1) : cref;
  return value.replace(/`[0-9]+/g, "");
}

function getOwningType(memberId) {
  const value = memberId.slice(2);
  const signatureStart = value.indexOf("(");
  const withoutSignature = signatureStart >= 0 ? value.slice(0, signatureStart) : value;
  const lastDot = withoutSignature.lastIndexOf(".");
  return lastDot >= 0 ? withoutSignature.slice(0, lastDot) : withoutSignature;
}

function compareMembers(left, right) {
  const leftKind = memberSortOrder(left.id[0]);
  const rightKind = memberSortOrder(right.id[0]);
  if (leftKind !== rightKind) {
    return leftKind - rightKind;
  }

  return left.id.localeCompare(right.id);
}

function memberSortOrder(kind) {
  switch (kind) {
    case "P":
      return 0;
    case "E":
      return 1;
    case "M":
      return 2;
    case "F":
      return 3;
    default:
      return 4;
  }
}

function renderTypePage(fullTypeName, simpleTypeName, typeMemberId, linkedMembers, docsByMember) {
  const typeDocs = docsByMember.get(typeMemberId) ?? emptyDocs();
  const grouped = new Map();

  for (const member of linkedMembers) {
    const kind = memberHeading(member.id[0]);
    const items = grouped.get(kind) ?? [];
    items.push(member);
    grouped.set(kind, items);
  }

  const lines = [
    `# \`${simpleTypeName}\``,
    "",
    typeDocs.summary || "No summary available.",
    "",
    "## Type",
    "",
    "```csharp",
    fullTypeName,
    "```"
  ];

  if (typeDocs.remarks) {
    lines.push("", "## Remarks", "", typeDocs.remarks);
  }

  for (const heading of ["Properties", "Events", "Methods", "Fields"]) {
    const items = grouped.get(heading);
    if (!items || items.length === 0) {
      continue;
    }

    lines.push("", `## ${heading}`);
    for (const item of items) {
      const itemDocs = docsByMember.get(item.id) ?? emptyDocs();
      lines.push("", `### \`${formatMemberSignature(item.id)}\``, "");
      lines.push(itemDocs.summary || "No summary available.");

      if (itemDocs.params.length > 0) {
        lines.push("", "**Parameters**", "");
        for (const parameter of itemDocs.params) {
          lines.push(`- \`${parameter.name}\`: ${parameter.description}`);
        }
      }

      if (itemDocs.returns) {
        lines.push("", `Returns: ${itemDocs.returns}`);
      }

      if (itemDocs.remarks) {
        lines.push("", itemDocs.remarks);
      }
    }
  }

  lines.push("", "[Back to API index](../index.md)");
  return `${lines.join("\n")}\n`;
}

function memberHeading(kind) {
  switch (kind) {
    case "P":
      return "Properties";
    case "E":
      return "Events";
    case "F":
      return "Fields";
    default:
      return "Methods";
  }
}

function formatMemberSignature(memberId) {
  const kind = memberId[0];
  const value = memberId.slice(2);
  const signatureStart = value.indexOf("(");
  const withoutSignature = signatureStart >= 0 ? value.slice(0, signatureStart) : value;
  const parameters = signatureStart >= 0
    ? value.slice(signatureStart + 1, value.lastIndexOf(")"))
    : "";
  const memberName = withoutSignature.slice(withoutSignature.lastIndexOf(".") + 1);

  if (kind === "P" || kind === "E" || kind === "F") {
    return memberName;
  }

  const displayName = memberName === "#ctor" ? "Constructor" : memberName;
  const parameterList = parameters.length === 0
    ? ""
    : parameters
      .split(",")
      .filter(Boolean)
      .map(parameter => simplifyTypeName(parameter.trim()))
      .join(", ");
  return `${displayName}(${parameterList})`;
}

function simplifyTypeName(typeName) {
  return typeName
    .replace(/System\./g, "")
    .replace(/Microsoft\.Extensions\.Logging\./g, "")
    .replace(/RoyalApps\.Community\.ExternalApps\.WinForms\./g, "")
    .replace(/RoyalApps\.Community\.ExternalApps\.WinForms\.WindowManagement\./g, "")
    .replace(/\{/g, "<")
    .replace(/\}/g, ">");
}

function slugify(value) {
  return value.replace(/[^A-Za-z0-9]+/g, "-").replace(/^-+|-+$/g, "").toLowerCase();
}

function renderApiIndex(namespaces, pagesByNamespace) {
  const lines = [
    "# API Reference",
    "",
    "Use the sidebar to browse namespaces and types."
  ];

  return `${lines.join("\n")}\n`;
}

function renderSidebarModule(namespaces, pagesByNamespace) {
  const groups = namespaces.map(namespaceName => ({
    text: formatNamespaceLabel(namespaceName),
    collapsed: false,
    items: (pagesByNamespace.get(namespaceName) ?? []).map(entry => ({
      text: entry.simpleTypeName,
      link: `/api/reference/${entry.slug}`
    }))
  }));

  return `export default ${JSON.stringify(groups, null, 2)};\n`;
}

function formatNamespaceLabel(namespaceName) {
  if (namespaceName === documentedNamespacePrefix) {
    return "ExternalApps.WinForms";
  }

  return namespaceName.replace(`${documentedNamespacePrefix}.`, "");
}

function emptyDocs() {
  return {
    summary: "",
    remarks: "",
    returns: "",
    params: []
  };
}

function isPublicMember(fullTypeName, memberId) {
  if (fullTypeName !== `${documentedNamespacePrefix}.ExternalAppHost`) {
    return true;
  }

  const allowedMembers = publicApi.publicMembers.get(fullTypeName);
  return allowedMembers ? allowedMembers.has(getMemberName(memberId)) : false;
}

function getMemberName(memberId) {
  const value = memberId.slice(2);
  const signatureStart = value.indexOf("(");
  const withoutSignature = signatureStart >= 0 ? value.slice(0, signatureStart) : value;
  const memberName = withoutSignature.slice(withoutSignature.lastIndexOf(".") + 1);
  return memberName;
}

function collectPublicApiSurface(sourceRoot) {
  const publicTypes = new Set();
  const publicMembers = new Map();

  for (const filePath of enumerateSourceFiles(sourceRoot)) {
    const content = readFileSync(filePath, "utf8");
    const namespaceMatch = content.match(/namespace\s+([A-Za-z0-9_.]+)\s*;/);
    if (!namespaceMatch) {
      continue;
    }

    const namespaceName = namespaceMatch[1];
    const lines = content.split(/\r?\n/);
    let braceDepth = 0;
    let currentType = null;

    for (const line of lines) {
      const trimmed = line.trim();
      const opens = countOccurrences(line, "{");
      const closes = countOccurrences(line, "}");

      const publicTypeMatch = trimmed.match(/^public\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+)*(class|enum|interface|struct)\s+(\w+)/);
      if (!currentType && publicTypeMatch) {
        const fullTypeName = `${namespaceName}.${publicTypeMatch[2]}`;
        currentType = {
          fullTypeName,
          simpleTypeName: publicTypeMatch[2],
          declarationDepth: braceDepth + 1,
          awaitingOpenBrace: opens === 0
        };
        publicTypes.add(fullTypeName);
        publicMembers.set(fullTypeName, new Set());
      }
      else if (currentType && trimmed.startsWith("public ")) {
        const memberName = tryParsePublicMemberName(trimmed, currentType.simpleTypeName);
        if (memberName) {
          publicMembers.get(currentType.fullTypeName)?.add(memberName);
        }
      }

      braceDepth += opens;
      if (currentType?.awaitingOpenBrace && braceDepth >= currentType.declarationDepth) {
        currentType.awaitingOpenBrace = false;
      }

      braceDepth -= closes;

      if (currentType && !currentType.awaitingOpenBrace && braceDepth < currentType.declarationDepth) {
        currentType = null;
      }
    }
  }

  return { publicTypes, publicMembers };
}

function enumerateSourceFiles(directory) {
  const results = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    if (entry.name === "bin" || entry.name === "obj") {
      continue;
    }

    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      results.push(...enumerateSourceFiles(fullPath));
    }
    else if (entry.isFile() && entry.name.endsWith(".cs")) {
      results.push(fullPath);
    }
  }

  return results;
}

function tryParsePublicMemberName(trimmedLine, simpleTypeName) {
  if (/^public\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+)*(class|enum|interface|struct)\s+/.test(trimmedLine)) {
    return null;
  }

  const eventMatch = trimmedLine.match(/^public\s+event\s+.+?\s+(\w+)\s*[;{]/);
  if (eventMatch) {
    return eventMatch[1];
  }

  const methodMatch = trimmedLine.match(/^public\s+.+?\b(\w+)\s*\(/);
  if (methodMatch) {
    return methodMatch[1] === simpleTypeName ? "#ctor" : methodMatch[1];
  }

  const propertyMatch = trimmedLine.match(/^public\s+.+?\b(\w+)\s*(?:=>|\{)/);
  if (propertyMatch) {
    return propertyMatch[1];
  }

  return null;
}

function countOccurrences(text, token) {
  return text.split(token).length - 1;
}
