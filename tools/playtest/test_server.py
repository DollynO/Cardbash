"""Protocol/error-path checks; run with python3 -m unittest discover -s tools/playtest."""
import io
import json
import unittest

from server import McpServer, PlaytestManager


class ProtocolTests(unittest.TestCase):
    def setUp(self):
        self.server = McpServer(PlaytestManager())

    def call(self, method, params=None):
        return self.server.handle({"jsonrpc": "2.0", "id": 1, "method": method, "params": params or {}})

    def initialize(self, version="2025-06-18"):
        return self.call("initialize", {"protocolVersion": version})

    def test_version_negotiation_and_tool_schemas(self):
        self.assertEqual(self.initialize("2099-01-01")["result"]["protocolVersion"], "2025-06-18")
        tools = self.call("tools/list")["result"]["tools"]
        self.assertEqual(len(tools), 6)
        for tool in tools:
            self.assertEqual(tool["inputSchema"]["type"], "object")

    def test_notifications_have_no_response(self):
        self.assertIsNone(self.server.handle({"jsonrpc": "2.0", "method": "notifications/initialized"}))

    def test_initialize_required(self):
        self.assertEqual(self.call("tools/list")["error"]["code"], -32000)

    def test_invalid_request_and_method(self):
        self.assertEqual(self.server.handle([])["error"]["code"], -32600)
        self.initialize()
        self.assertEqual(self.call("missing")["error"]["code"], -32601)

    def test_tool_failures_are_results_and_server_remains_usable(self):
        self.initialize()
        error = self.call("tools/call", {"name": "playtest_command", "arguments": {
            "instance_id": "unknown", "command": "state"}})
        self.assertTrue(error["result"]["isError"])
        self.assertEqual(self.call("tools/call", {"name": "playtest_instances"})["result"]["content"][0]["text"], "[]")

    def test_stdio_parse_error_and_eof_cleanup(self):
        output = io.StringIO()
        self.server.serve(io.StringIO('bad json\n{"jsonrpc":"2.0","id":2,"method":"ping"}\n'), output)
        responses = [json.loads(line) for line in output.getvalue().splitlines()]
        self.assertEqual(responses[0]["error"]["code"], -32700)
        self.assertEqual(responses[1]["result"], {})


if __name__ == "__main__":
    unittest.main()
