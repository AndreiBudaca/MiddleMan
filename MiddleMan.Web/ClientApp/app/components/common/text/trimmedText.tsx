import { Tooltip, Typography } from "@mui/material";

export interface TrimmedTextProps {
  text: string;
  maxLength: number;
}

export function TrimmedText({ text, maxLength }: TrimmedTextProps) {
  const displayText =
    text.length > maxLength ? `${text.slice(0, maxLength)}...` : text;

  return (
    <Tooltip title={text}>
      <Typography variant="body1">{displayText}</Typography>
    </Tooltip>
  );
}
