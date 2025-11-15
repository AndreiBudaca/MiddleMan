import { Box, Typography } from "@mui/material";
import { RichTreeView, type TreeViewBaseItem } from "@mui/x-tree-view";
import { mapToTreeNodes } from "~/mappers/clientMapper";
import type { ClientWithMethods } from "~/contracts/client";

export interface ClientTreeViewProps {
  clients: ClientWithMethods[];
  onNodeSelected: (path: string) => void;
}

export default function ClientTreeView({
  clients,
  onNodeSelected,
}: ClientTreeViewProps) {

  return (
    <Box sx={{ minHeight: 352, minWidth: 250 }}>
      {clients.length > 0 ?
        <RichTreeView items={mapToTreeNodes(clients)} onItemClick={(e, id) => onNodeSelected(id)} /> :
        <Typography variant="body1">No clients available at the moment</Typography>
      }
    </Box>
  );
}
