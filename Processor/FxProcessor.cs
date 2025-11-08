using OwnaudioLegacy.Processors;
using System;
using System.Collections.Generic;

namespace OwnaAvalonia.Processor
{
    /// <summary>
    /// Processes audio samples through a chain of sample processors.
    /// </summary>
    /// <remarks>
    /// This class manages a collection of <see cref="SampleProcessorBase"/> instances
    /// and applies them sequentially to the audio samples during processing.
    /// </remarks>
    public class FxProcessor : SampleProcessorBase
    {            
        private List<SampleProcessorBase>  _sampleProcessor = new List<SampleProcessorBase>();

        /// <summary>
        /// Adds an effect processor to the processing chain.
        /// </summary>
        /// <param name="fx">The sample processor to add to the chain.</param>
        public void AddFx(SampleProcessorBase fx)
        {
           _sampleProcessor.Add(fx);
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Reset()
        {
            if (_sampleProcessor.Count > 0)
            {
                foreach (SampleProcessorBase fx in _sampleProcessor)
                {
                    fx.Reset();
                }
            }
        }
        
        /// <summary>
        /// Processes the audio samples by applying all registered effect processors in sequence.
        /// </summary>
        /// <param name="sample">The span of audio samples to process.</param>
        /// <remarks>
        /// Each processor in the chain will modify the samples in place.
        /// </remarks>
        public override void Process(Span<float> sample)
        { 
            foreach( var fxProcessor in _sampleProcessor)
                fxProcessor.Process(sample);
        }
    }
}
