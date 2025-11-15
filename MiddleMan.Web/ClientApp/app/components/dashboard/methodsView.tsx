import { Box, Button, Typography } from "@mui/material";
import { JsonEditor, githubDarkTheme } from "json-edit-react";
import type { ClientMethod } from "~/contracts/clientMethods";
import CodeEditor from "./jsonView/codeEditor";
import { useEffect, useState } from "react";
import { mapToJsonTemplate } from "~/mappers/clientMethodArgumentsMapper";
import { callClientMethod } from "~/services/clients/clientService";

export interface MethodsViewProps {
  client: string | null | undefined;
  method: ClientMethod | null;
  isCallable: boolean,
}

export default function MethodsView({ client, method, isCallable }: MethodsViewProps) {
  const [methodParams, setMethodParams] = useState<any>(null);
  const [result, setResult] = useState<any>(null);

  useEffect(() => {
    if (!method || method.arguments.length == 0) {
      setMethodParams(null);
    } else {
      setMethodParams(mapToJsonTemplate(method));
    }

    setResult(null);
  }, [method]);

  const invokeMethod = async (_: any) => {
    if (!method || !client) return;
    const result = await callClientMethod(client, method?.name, methodParams);
    setResult(result);
  };

  const displayResult = result != null && result != undefined;

  return (
    <Box minWidth="300px" width="60%">
      {method && <Typography variant="body1">{method.name}</Typography>}
      {methodParams && (
        <JsonEditor
          data={methodParams}
          setData={setMethodParams}
          theme={githubDarkTheme}
          rootName="Arguments"
          TextEditor={(props) => <CodeEditor {...props} />}
          maxWidth="100vw"
        />
      )}
      {method && isCallable && (
        <Button variant="contained" onClick={invokeMethod}>
          Call
        </Button>
      )}
      {displayResult && (
        <JsonEditor
          data={result}
          theme={githubDarkTheme}
          rootName="Result"
          maxWidth="100vw"
          restrictAdd={true}
          restrictEdit={true}
          restrictDelete={true}
          restrictDrag={true}
          restrictTypeSelection={true}
        />
      )}
    </Box>
  );
}
