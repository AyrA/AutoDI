using System;
using System.Linq;

namespace AyrA.AyrA.AutoDI
{
    /// <summary>
    /// The base attribute for AutoDI that provides filtering
    /// </summary>
    public abstract class AutoDIFilterAttribute : Attribute
    {
        /// <summary>
        /// The list of filters
        /// </summary>
        private string[] filters = [];

        /// <summary>
        /// Gets or sets the filter list
        /// </summary>
        /// <remarks>
        /// The filter list is a comma separated list of strings.
        /// A filter may be prefixed with "!" to convert it into an exclusion filter
        /// </remarks>
        public string Filters
        {
            get => string.Join(",", filters ?? []);
            set => filters = [.. (value ?? "").Split(",").Select(m => m.Trim().ToLowerInvariant()).Distinct()];
        }

        /// <summary>
        /// Tests if the type should be added to the service collection,
        /// based on <see cref="Filters"/> and <paramref name="filterList"/>
        /// </summary>
        /// <param name="filterList">Accepted filters</param>
        /// <returns>true, if should be added</returns>
        /// <remarks>
        /// Also returns true if <paramref name="filterList"/> or <see cref="Filters"/> is empty
        /// </remarks>
        internal bool IsFilterMatch(string[] filterList)
        {
            ArgumentNullException.ThrowIfNull(filterList);
            if (filters.Length == 0 || filterList.Length == 0)
            {
                return true;
            }

            var excludes = filters.Where(m => m.StartsWith('!')).ToArray();
            var includes = filters.Except(excludes).ToArray();

            //If at least one exclusion matches, do not include the service
            if (excludes.Select(m => m[1..]).Any(filterList.Contains))
            {
                return false;
            }
            //If at least one inclusion matches, include the service
            return includes.Any(filterList.Contains);
        }
    }
}
