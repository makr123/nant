// NAnt - A .NET build tool
// Copyright (C) 2001-2003 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Tomas Restrepo (tomasr@mvps.org)

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;

using NUnit.Core;

namespace NAnt.NUnit2.Tasks {
    /// <summary>
    /// Custom TestDomain, similar to the one included with NUnit, in order 
    /// to workaround some limitations in it.
    /// </summary>
    internal class NUnit2TestDomain {
        #region Public Instance Constructors

        public NUnit2TestDomain(TextWriter outStream, TextWriter errorStream) {
            _outStream = outStream;
            _errorStream = errorStream;
        }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Runs a single testcase.
        /// </summary>
        /// <param name="testcase">The test to run, or <see langword="null" /> if running all tests.</param>
        /// <param name="assemblyFile">The test assembly.</param>
        /// <param name="configFile">The application configuration file for the test domain.</param>
        /// <param name="listener">An <see cref="EventListener" />.</param>
        /// <returns>
        /// The result of the test.
        /// </returns>
        public TestResult RunTest(string testcase, FileInfo assemblyFile, FileInfo configFile, EventListener listener) {
            // create test domain
            AppDomain domain = CreateDomain(assemblyFile.Directory, configFile);

            // assemble directories which can be probed for missing unresolved 
            // assembly references
            StringCollection probePaths = new StringCollection();
            
            if (AppDomain.CurrentDomain.SetupInformation.PrivateBinPath != null) {
                foreach (string path in AppDomain.CurrentDomain.SetupInformation.PrivateBinPath.Split(';')) {
                    // path is relative to base directory of AppDomain, so 
                    // resolve to full path
                    probePaths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
                }
            }

            // add base directory of current AppDomain as probe path
            probePaths.Add(AppDomain.CurrentDomain.BaseDirectory);

            // create an instance of our custom Assembly Resolver in the target domain.
            ObjectHandle oh = domain.CreateInstanceFrom(Assembly.GetExecutingAssembly().CodeBase, 
                    typeof(AssemblyResolveHandler).FullName,
                    false, 
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new object[] {probePaths},
                    CultureInfo.InvariantCulture,
                    null,
                    AppDomain.CurrentDomain.Evidence);     
            
            // store current directory
            string currentDir = Directory.GetCurrentDirectory();

            // change current dir to directory containing test assembly
            Directory.SetCurrentDirectory(assemblyFile.DirectoryName);

            try {
                // create testrunner
                RemoteTestRunner runner = CreateTestRunner(domain);

                // set the file name of the test assembly without directory 
                // information, as the current directory is already set to the 
                // directory containing the assembly
                runner.TestFileName = assemblyFile.Name;

                // check whether an individual testcase should be run
                if (testcase != null) {
                    runner.TestName = testcase;
                }

                // build the test suite
                runner.BuildSuite();

                // run the test(s)
                return runner.Run(listener, _outStream, _errorStream);
            } finally {
                // restore original current directory
                Directory.SetCurrentDirectory(currentDir);

                // unload test domain
                AppDomain.Unload(domain);
            }
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private AppDomain CreateDomain(DirectoryInfo basedir, FileInfo configFile) {
            // spawn new domain in specified directory
            AppDomainSetup domSetup = new AppDomainSetup();
            domSetup.ApplicationBase = basedir.FullName;
            if (configFile != null) {
                domSetup.ConfigurationFile = configFile.FullName;
            }
            domSetup.ApplicationName = "NAnt NUnit Remote Domain";
         
            return AppDomain.CreateDomain( 
                domSetup.ApplicationName, 
                AppDomain.CurrentDomain.Evidence, 
                domSetup
                );
        }

        private RemoteTestRunner CreateTestRunner(AppDomain domain) {
            ObjectHandle oh;
            Type rtrType = typeof(RemoteTestRunner);

            oh = domain.CreateInstance(
                rtrType.Assembly.FullName,
                rtrType.FullName,
                false, 
                BindingFlags.Public | BindingFlags.Instance, 
                null,
                null,
                CultureInfo.InvariantCulture,
                null,
                AppDomain.CurrentDomain.Evidence);
            return (RemoteTestRunner) oh.Unwrap();
        }

        #endregion Private Instance Methods

        #region Private Instance Fields

        private TextWriter _outStream;
        private TextWriter _errorStream;

        #endregion Private Instance Fields

        /// <summary>
        /// Helper class called when an assembly resolve event is raised.
        /// </summary>
        [Serializable()]
        private class AssemblyResolveHandler {
            #region Private Instance Fields

            private StringCollection _probePaths;

            #endregion Private Instance Fields
        
            #region Public Instance Constructors

            /// <summary> 
            /// Initializes an instanse of the <see cref="AssemblyResolveHandler" /> 
            /// class.
            /// </summary>
            public AssemblyResolveHandler(StringCollection probePaths) {
                _probePaths = probePaths;
            
                // attach our handler for the current domain.
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssembly);
            }

            #endregion Public Instance Constructors

            #region Public Instance Methods

            /// <summary>
            /// Called back when the CLR cannot resolve a given assembly.
            /// </summary>
            /// <param name="sender">The source of the event.</param>
            /// <param name="args">A <see cref="ResolveEventArgs" /> that contains the event data.</param>
            /// <returns>
            /// The <c>nunit.framework</c> we know to be in NAnts bin directory, if 
            /// that is the assembly that needs to be resolved; otherwise, 
            /// <see langword="null" />.
            /// </returns>
            public Assembly ResolveAssembly(Object sender, ResolveEventArgs args) {
                foreach (string path in _probePaths) {
                    if (!Directory.Exists(path)) {
                        continue;
                    }

                    string[] assemblies = Directory.GetFiles(path, "*.dll");

                    foreach (string assemblyFile in assemblies) {
                        try {
                            AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyFile);
                            if (assemblyName.FullName == args.Name) {
                                return Assembly.LoadFrom(assemblyFile);
                            }
                        } catch {}
                    }
                }
                return null;
            }

            #endregion Public Instance Methods
        }
    }
}
