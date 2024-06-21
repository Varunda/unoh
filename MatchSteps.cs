using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using unoh.step;

namespace unoh {

    public class MatchSteps {

        private readonly ILogger<MatchSteps> _Logger;
        private Dictionary<string, IFlipStep> _Steps { get; set; } = [];

        public MatchSteps(ILogger<MatchSteps> logger) {
            _Logger = logger;

            List<Type> steps = Assembly.GetAssembly(typeof(Program))!.GetTypes()
                .Where(iter => iter.IsAssignableTo(typeof(IFlipStep)) && iter.IsInterface == false)
                .ToList();
            _Logger.LogInformation($"loading {steps.Count} steps");

            foreach (Type t in steps) {
                _Logger.LogDebug($"attemping to create IFlipStep [t.FullName={t.FullName}]");
                IFlipStep? step = (IFlipStep?) Activator.CreateInstance(t);
                if (step == null) {
                    _Logger.LogError($"failed to create t={t.FullName}");
                } else {
                    _Steps.Add(step.Name.ToLower().Trim(), step);
                    _Logger.LogDebug($"added step [step.Name={step.Name}]");
                }
            }
        }

        /// <summary>
        ///     get a <see cref="IFlipStep"/> based on <see cref="IFlipStep.Name"/> (case-insensitive)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IFlipStep? GetStep(string name) {
            return _Steps.GetValueOrDefault(name.ToLower().Trim());
        }

    }
}
