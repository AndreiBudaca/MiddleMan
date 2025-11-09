import { Box } from "@mui/material";
import { SimpleTreeView, TreeItem } from "@mui/x-tree-view";

export interface ClientTreeViewProps 
{
    nodes: TreeNode[];
}

export interface TreeNode {
  label: string;
  children?: TreeNode[];
}

export default function ClientTreeView({ nodes }: ClientTreeViewProps) {
  return (
    <Box sx={{ minHeight: 352, minWidth: 250 }}>
      <SimpleTreeView>{renderTree(nodes)}</SimpleTreeView>
    </Box>
  );
}

function renderTree(nodes: TreeNode[], selector: string = "") {
  const currentSelector = (node: TreeNode) => `${selector}-${node.label}`;

  return nodes.map((node, index) =>
    node.children && node.children.length > 0 ? (
      <TreeItem key={index} itemId={currentSelector(node)} label={node.label}>
        {renderTree(node.children, currentSelector(node))}
      </TreeItem>
    ) : (
      <TreeItem key={index} itemId={currentSelector(node)} label={node.label} />
    )
  );
}
