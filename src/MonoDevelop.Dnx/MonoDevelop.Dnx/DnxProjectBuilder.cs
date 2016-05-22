﻿//
// DnxProjectBuilder.cs
//
// Author:
//       Matt Ward <ward.matt@gmail.com>
//
// Copyright (c) 2015 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Threading;
using Microsoft.DotNet.ProjectModel.Server.Models;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Dnx
{
	public class DnxProjectBuilder : IDisposable
	{
		DnxProject project;
		IProgressMonitor monitor;
		ManualResetEventSlim waitEvent = new ManualResetEventSlim ();
		bool cancelled;
		DiagnosticsListMessage[] messages;

		public DnxProjectBuilder (DnxProject project, IProgressMonitor monitor)
		{
			this.project = project;
			this.monitor = monitor;
			this.monitor.CancelRequested += CancelRequested;
		}

		public string ProjectPath {
			get { return project.JsonPath; }
		}

		void CancelRequested (IProgressMonitor monitor)
		{
			cancelled = true;
			waitEvent.Set ();
		}

		public void Dispose ()
		{
			IProgressMonitor currentMonitor = monitor;
			if (currentMonitor != null) {
				currentMonitor.CancelRequested -= CancelRequested;
				monitor = null;
			}
		}

		public BuildResult Build ()
		{
			if (!DnxServices.ProjectService.HasCurrentDnxRuntime)
				return CreateDnxRuntimeErrorBuildResult ();

			if (project.JsonPath != null) {
				DnxServices.ProjectService.GetDiagnostics (this);
			} else {
				return CreateDnxProjectNotInitializedBuildResult ();
			}

			waitEvent.Wait ();

			if (cancelled || messages == null) {
				return new BuildResult ();
			}
			return CreateBuildResult ();
		}

		BuildResult CreateDnxRuntimeErrorBuildResult ()
		{
			var buildResult = new BuildResult ();
			buildResult.AddError (DnxServices.ProjectService.CurrentRuntimeError);
			return buildResult;
		}

		BuildResult CreateDnxProjectNotInitializedBuildResult ()
		{
			var buildResult = new BuildResult ();
			buildResult.AddError (String.Format ("Project '{0}' has not been initialized by .NET Core host.", project.Name));
			return buildResult;
		}

		public void OnDiagnostics (DiagnosticsListMessage[] messages)
		{
			this.messages = messages;
			waitEvent.Set ();
		}

		BuildResult CreateBuildResult ()
		{
			foreach (DiagnosticsListMessage message in messages) {
				if (project.CurrentFramework == message.Framework.FrameworkName) {
					return message.ToBuildResult (project);
				}
			}
			return new BuildResult ();
		}
	}
}

