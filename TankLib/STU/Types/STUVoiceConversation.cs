// Generated by TankLibHelper

// ReSharper disable All
namespace TankLib.STU.Types
{
    [STU(0xA408D74F, 56)]
    public class STUVoiceConversation : STUInstance
    {
        [STUField(0x401F5484, 8)] // size: 16
        public teStructuredDataAssetRef<STUVoiceStimulus> m_stimulus;
        
        [STUField(0x90D76F17, 24, ReaderType = typeof(InlineInstanceFieldReader))] // size: 16
        public STUVoiceConversationLine[] m_90D76F17;
        
        [STUField(0x4FF98D41, 40, ReaderType = typeof(EmbeddedInstanceFieldReader))] // size: 8
        public STUCriteriaContainer m_criteria;
        
        [STUField(0x9CDDC24D, 48)] // size: 4
        public float m_weight = 1f;
        
        [STUField(0x98F0E612, 52)] // size: 1
        public byte m_98F0E612 = 0x0;
    }
}
