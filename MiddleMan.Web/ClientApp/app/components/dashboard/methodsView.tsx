import {
  Box,
  Button,
  createTheme,
  TextField,
  ThemeProvider,
  Typography,
} from "@mui/material";
import { JsonEditor, githubDarkTheme } from "json-edit-react";
import type { ClientMethod } from "~/contracts/clientMethods";
import CodeEditor from "./jsonView/codeEditor";
import { useEffect, useState } from "react";
import { mapToJsonTemplate } from "~/mappers/clientMethodArgumentsMapper";
import { callClientMethod } from "~/services/clients/clientService";

export interface MethodsViewProps {
  client: string | null | undefined;
  method: ClientMethod | null;
  isCallable: boolean;
}

export default function MethodsView({
  client,
  method,
  isCallable,
}: MethodsViewProps) {
  const [methodParams, setMethodParams] = useState<any>(null);
  const [result, setResult] = useState<any>(null);

  const displayResult = result != null && result != undefined;
  const isBinaryMethod =
    method?.arguments.length == 1 && method?.arguments[0].isBinary;
  const isBinaryResult = method?.returns?.isBinary ?? false;

  console.log(methodParams);

  const darkTheme = createTheme({
    palette: {
      mode: "dark",
    },
  });

  useEffect(() => {
    if (!method || method.arguments.length == 0 || isBinaryMethod) {
      setMethodParams(null);
    } else {
      setMethodParams(mapToJsonTemplate(method));
    }

    setResult(null);
  }, [method]);

  const invokeMethod = async (_: any) => {
    if (!method || !client) return;
    const result = await callClientMethod(
      client,
      method?.name,
      isBinaryMethod,
      isBinaryResult,
      methodParams
    );
    setResult(result);
  };

  return (
    <ThemeProvider theme={darkTheme}>
      <Box minWidth="300px" width="60%">
        {method && <Typography variant="body1">{method.name}</Typography>}
        {methodParams && !isBinaryMethod && (
          <JsonEditor
            data={methodParams}
            setData={setMethodParams}
            theme={githubDarkTheme}
            rootName="Arguments"
            TextEditor={(props) => <CodeEditor {...props} />}
            maxWidth="100vw"
          />
        )}
        {isBinaryMethod && (
          <TextField
            type="file"
            variant="filled"
            fullWidth
            margin="normal"
            onChange={(e) => setMethodParams((e.target as any).files[0])}
          />
        )}
        {method && isCallable && (
          <Box marginTop="15px">
            <Button variant="contained" onClick={invokeMethod}>
              Call
            </Button>
          </Box>
        )}
        {displayResult && !isBinaryResult && (
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
        {displayResult && isBinaryMethod && (
          <a href={result} target="blank">
            <Typography variant="body1">Download result</Typography>
          </a>
        )}
      </Box>
    </ThemeProvider>
  );
}
