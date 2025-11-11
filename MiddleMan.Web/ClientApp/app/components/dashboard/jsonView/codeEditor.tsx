import AceEditor from "react-ace";

import "ace-builds/src-noconflict/mode-json";
import "ace-builds/src-noconflict/theme-pastel_on_dark";
import "ace-builds/src-noconflict/ext-language_tools";
import type { TextEditorProps } from "json-edit-react";

const CodeEditor = ({ value, onChange }: TextEditorProps) => {
  return (
    <AceEditor
      mode="json"
      theme="pastel_on_dark"
      onChange={onChange}
      name="code_editor"
      value={value}
      width="100%"
    />
  );
};

export default CodeEditor;
