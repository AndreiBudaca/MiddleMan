import { Box } from "@mui/material";
import { RichTreeView, type TreeViewBaseItem } from "@mui/x-tree-view";
import { mapToTreeNodes } from "~/mappers/clientMapper";
import type { ClientWithMethods } from "~/services/clients/contracts/client";

export interface ClientTreeViewProps {
  clients: ClientWithMethods[];
  onNodeSelected: (path: string) => void;
}

export interface TreeNode {
  label: string;
  children?: TreeNode[];
}

export default function ClientTreeView({
  clients,
  onNodeSelected,
}: ClientTreeViewProps) {

  const mapNode = (node: TreeNode, selector: string | null = null): TreeViewBaseItem => {
    const currentSelector = selector ? `${selector}/${node.label}` : node.label;
    return {
      id: currentSelector,
      label: node.label,
      children: node.children?.map(c => mapNode(c, currentSelector))
    }
  }

  return (
    <Box sx={{ minHeight: 352, minWidth: 250 }}>
      <RichTreeView items={mapToTreeNodes(clients)} onItemClick={(e, id) => onNodeSelected(id)} />
    </Box>
  );
}
