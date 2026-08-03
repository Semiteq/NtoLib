using System;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// One link's connect unit, decoupled from the vendor COM surface so the connect pass is unit-testable
/// without a live <c>IProjectHlp</c>. <see cref="Connect"/> performs the vendor connect. There is
/// deliberately no read-back member — success is judged by the SCADA tree on reload, never in code.
/// </summary>
internal readonly record struct ConnectCommand(
	string LinkType,
	string LocalPinPath,
	string ExternalPinPath,
	Action Connect);
