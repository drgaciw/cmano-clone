#!/usr/bin/env node
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';
import { existsSync, readdirSync } from 'node:fs';
import { homedir } from 'node:os';
import { join } from 'node:path';

function findDesktopCommanderEntry() {
  const npxRoot = join(homedir(), 'AppData', 'Local', 'npm-cache', '_npx');
  if (!existsSync(npxRoot)) return null;
  for (const dir of readdirSync(npxRoot)) {
    const candidate = join(
      npxRoot,
      dir,
      'node_modules',
      '@wonderwhy-er',
      'desktop-commander',
      'dist',
      'index.js',
    );
    if (existsSync(candidate)) return candidate;
  }
  return null;
}

const toolName = process.argv[2];
if (!toolName) {
  console.error('Usage: node invoke-desktop-commander.mjs <toolName> [jsonArgs]');
  process.exit(1);
}

const args = process.argv[3] ? JSON.parse(process.argv[3]) : {};
const entry = findDesktopCommanderEntry();
if (!entry) {
  console.error('desktop-commander not cached. Run: npx -y @wonderwhy-er/desktop-commander@latest');
  process.exit(1);
}

const transport = new StdioClientTransport({
  command: process.execPath,
  args: [entry],
  cwd: process.cwd(),
});

const client = new Client({ name: 'cmano-clone-verify', version: '1.0.0' });

await client.connect(transport);
try {
  const result = await client.callTool({ name: toolName, arguments: args });
  console.log(JSON.stringify(result, null, 2));
} finally {
  await client.close().catch(() => {});
}