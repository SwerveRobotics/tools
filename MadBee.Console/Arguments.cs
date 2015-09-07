using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MadBee.Console
    {
    public class Arguments : IEnumerable<string>
        {

        /// <summary>
        /// Initializes a new instance of the <see cref="Arguments"/> class.
        /// </summary>
        /// <param name="args">The args.</param>
        public Arguments(string[] args) : this()
            {
            Parse(args);
            }

        /// <summary>
        /// Initializes a new instance of the <see cref="Arguments"/> class.
        /// </summary>
        public Arguments()
            {
            Parameters = new Dictionary<string, string>();
            }

        private Dictionary<string, string> Parameters
            {
            get; set;
            }

        /// <summary>
        /// Parses the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        public void Parse(string[] args)
            {
            Parameters.Clear();

            Regex spliter = new Regex(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

            Regex remover = new Regex(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

            string parameter = null;
            string[] parts;

            // Valid parameters forms:
            // {-,/,--}param{ ,=,:}((",')value(",'))
            // Examples: 
            // -param1 value1 --param2 /param3:"Test-:-work" 
            //   /param4=happy -param5 '--=nice=--'
            foreach (string txt in args)
                {
                // Look for new parameters (-,/ or --) and a
                // possible enclosed value (=,:)
                parts = spliter.Split(txt, 3);

                switch (parts.Length)
                    {
                // Found a value (for the last parameter 
                // found (space separator))
                case 1:
                    if (parameter != null)
                        {
                        if (!Parameters.ContainsKey(parameter))
                            {
                            parts[0] = remover.Replace(parts[0], "$1");
                            Parameters.Add(parameter, parts[0]);
                            }
                        parameter = null;
                        }
                    else
                        {
                        parts[0] = remover.Replace(parts[0], "$1");
                        if (!Parameters.ContainsKey(parts[0].ToLower().Trim()))
                            {
                            Parameters.Add(parts[0].ToLower().Trim(), parts[0]);
                            }
                        }
                    // else Error: no parameter waiting for a value (skipped)
                    break;

                // Found just a parameter
                case 2:
                    // The last parameter is still waiting. 
                    // With no value, set it to true.
                    if (parameter != null)
                        {
                        if (!Parameters.ContainsKey(parameter))
                            Parameters.Add(parameter, "true");
                        }
                    parameter = parts[1];
                    break;

                // Parameter with enclosed value
                case 3:
                    // The last parameter is still waiting. 
                    // With no value, set it to true.
                    if (parameter != null)
                        {
                        if (!Parameters.ContainsKey(parameter))
                            Parameters.Add(parameter, "true");
                        }

                    parameter = parts[1];

                    // Remove possible enclosing characters (",')
                    if (!Parameters.ContainsKey(parameter))
                        {
                        parts[2] = remover.Replace(parts[2], "$1");
                        Parameters.Add(parameter, parts[2]);
                        }

                    parameter = null;
                    break;
                    }
                }
            // In case a parameter is still waiting
            if (parameter != null)
                {
                if (!Parameters.ContainsKey(parameter))
                    Parameters.Add(parameter, "true");
                }
            }

        // Retrieve a parameter value if it exists 
        // (overriding C# indexer property)
        /// <summary>
        /// Gets the <see cref="System.String"/> with the specified param.
        /// </summary>
        /// <value></value>
        /// <exception cref="IndexOutOfRangeException">If it does not contain the specified param.</exception>
        public string this[string param]
            {
            get
                {
                return Get(param);
                }
            }

        /// <summary>
        /// Gets the <see cref="System.String"/> with the specified paramlist.
        /// </summary>
        /// <value></value>
        /// <exception cref="IndexOutOfRangeException">If it does not contain the specified param.</exception>
        public string this[params string[] paramlist]
            {
            get
                {
                return Get(paramlist);
                }
            }


        /// <summary>
        /// Gets the specified param value.
        /// </summary>
        /// <param name="param">The param.</param>
        /// <returns>The value of the param.</returns>
        /// <exception cref="IndexOutOfRangeException">If it does not contain the specified param.</exception>
        public string Get(string param)
            {
            if (ContainsKey(param))
                {
                return Parameters[param];
                }

            throw new IndexOutOfRangeException();
            }

        /// <summary>
        /// Gets the value of a param from the specified paramlist.
        /// </summary>
        /// <param name="paramlist">The paramlist.</param>
        /// <returns>The value of the param.</returns>
        /// <exception cref="IndexOutOfRangeException">If it does not contain the specified param.</exception>
        public string Get(params string[] paramlist)
            {
            foreach (var param in paramlist)
                {
                if (Parameters.ContainsKey(param))
                    {
                    return Parameters[param];
                    }
                }

            throw new IndexOutOfRangeException();
            }

        /// <summary>
        /// Determines whether this instance contains the specified key.
        /// </summary>
        /// <param name="param">The param.</param>
        /// <returns>
        /// 	<c>true</c> if this instance contains the specified key; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(string param)
            {
            return Parameters.ContainsKey(param);
            }

        /// <summary>
        /// Determines whether this instance contains any of the params in the specified param list.
        /// </summary>
        /// <param name="paramlist">The paramlist.</param>
        /// <returns>
        /// 	<c>true</c> if this instance contains any of the params in the specified param list; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsKey(params string[] paramlist)
            {
            foreach (var item in paramlist)
                {
                if (Parameters.ContainsKey(item))
                    {
                    return true;
                    }
                }
            return false;
            }

        /// <summary>
        /// Determines whether the specified value contains value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// 	<c>true</c> if the specified value contains value; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsValue(string value)
            {
            return Parameters.ContainsValue(value);
            }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public Dictionary<string, string>.KeyCollection Keys
            {
            get
                {
                return Parameters.Keys;
                }
            }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public Dictionary<string, string>.ValueCollection Values
            {
            get
                {
                return this.Parameters.Values;
                }
            }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count
            {
            get
                {
                return this.Parameters.Count;
                }
            }



        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
            {
            StringBuilder sb = new StringBuilder();
            foreach (string key in Parameters.Keys)
                sb.AppendFormat("/{0}={1} ", key, Parameters[key]);
            return sb.ToString().Trim();
            }

        public IEnumerator<string> GetEnumerator()
            {
            return this.Keys.GetEnumerator();
            }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
            return this.GetEnumerator();
            }
        }
    }
