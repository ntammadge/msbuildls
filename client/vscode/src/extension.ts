/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';
import {
	LanguageClient,
	LanguageClientOptions,
	ServerOptions,
	TransportKind
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	// The server is implemented in node
	const serverModule = context.asAbsolutePath(
		path.join('src/msbuildls/bin/debug/net8.0', 'msbuildls.exe')
	);

	// If the extension is launched in debug mode then the debug server options are used
	// Otherwise the run options are used
	const serverOptions: ServerOptions = {
		run: {
			command: serverModule,
			transport: TransportKind.stdio
		},
		debug: {
			command: serverModule,
			transport: TransportKind.stdio,
			args: ['--debug']
		}
	};

	// Options to control the language client
	const clientOptions: LanguageClientOptions = {
		// Register the server for xml documents with well-known msbuild file extensions
		documentSelector: [
			{
				language: 'xml',
				pattern: '{**/*.props,**/*.targets,**/*.*proj}'
			}
		],
		synchronize: {
			// Notify the server about file changes to '.clientrc files contained in the workspace
			fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
		}
	};

	// Create the language client and start the client.
	client = new LanguageClient(
		'msbuildls',
		'MSBuild Language Server',
		serverOptions,
		clientOptions
	);

	// Start the client. This will also launch the server
	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}