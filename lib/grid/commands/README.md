# Grid.Commands

This provides a way to interact with the Grid Server's commands.
This can only be used in Grid Server implementations that replaced Lua in their script executions with
the JSON commands provided by this library.

All pre-json implementations of the Grid Server should change integrations using this library to use the
old Lua-based commands, as we do not provide backwards compatibility for the old commands.

## License

This project is licensed under the Apache-2.0 License:

```
   Copyright 2024 MFDLABS

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

```