using System;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// One link's connect unit, decoupled from vendor COM so the connect pass is testable without a live
/// <c>IProjectHlp</c>. <see cref="Connect"/> performs the vendor connect. No read-back member exists
/// on purpose: success is judged by the SCADA tree on reload, never in code.
/// </summary>
internal readonly record struct ConnectCommand(
	string LinkType,
	string LocalPinPath,
	string ExternalPinPath,
	Action Connect);
