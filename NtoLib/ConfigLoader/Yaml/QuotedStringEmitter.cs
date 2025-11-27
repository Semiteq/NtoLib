using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace NtoLib.ConfigLoader.Yaml;

public class QuotedStringEmitter : ChainedEventEmitter
{
	private bool _isValuePosition;

	public QuotedStringEmitter(IEventEmitter nextEmitter)
		: base(nextEmitter)
	{
	}

	public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
	{
		if (_isValuePosition && eventInfo.Source.Type == typeof(string))
		{
			eventInfo.Style = ScalarStyle.DoubleQuoted;
		}

		_isValuePosition = !_isValuePosition;

		base.Emit(eventInfo, emitter);
	}

	public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
	{
		_isValuePosition = false;
		base.Emit(eventInfo, emitter);
	}
}
