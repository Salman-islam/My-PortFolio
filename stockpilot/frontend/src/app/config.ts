export interface Field {
  key: string;
  label: string;
  type: string;
  source?: string;
  display?: string;
  options?: any[];
  min?: number;
  max?: number;
  step?: string;
  minlength?: number;
  createOnly?: boolean;
  optional?: boolean;
}
export interface Page {
  key: string;
  title: string;
  columns: { key: string; label: string }[];
  fields: Field[];
  roles?: string[];
  createRoles?: string[];
  editRoles?: string[];
  deleteRoles?: string[];
  filter?: string;
}
export const config: {
  slug: string;
  title: string;
  tag: string;
  color: string;
  description: string;
  primary: string;
  pages: Page[];
} = {
  slug: "stockpilot",
  title: "StockPilot",
  tag: "Inventory & sales",
  color: "#008b77",
  description: "Know your stock. Keep every sale in balance.",
  primary: "products",
  pages: [
    {
      key: "products",
      title: "Products",
      columns: [
        {
          key: "name",
          label: "Product",
        },
        {
          key: "sku",
          label: "SKU",
        },
        {
          key: "category",
          label: "Category",
        },
        {
          key: "price",
          label: "Unit price",
        },
        {
          key: "stock",
          label: "On hand",
        },
        {
          key: "reorderLevel",
          label: "Reorder at",
        },
        {
          key: "active",
          label: "Active",
        },
      ],
      fields: [
        {
          key: "name",
          label: "Name",
          type: "text",
        },
        {
          key: "sku",
          label: "SKU",
          type: "text",
        },
        {
          key: "category",
          label: "Category",
          type: "text",
        },
        {
          key: "price",
          label: "Unit price",
          type: "number",
          min: 0,
          step: ".01",
        },
        {
          key: "reorderLevel",
          label: "Reorder level",
          type: "number",
          min: 0,
        },
        {
          key: "active",
          label: "Active",
          type: "select",
          options: [true, false],
        },
      ],
      createRoles: ["Admin"],
      editRoles: ["Admin"],
      deleteRoles: ["Admin"],
      filter: "category",
    },
    {
      key: "parties",
      title: "Customers & suppliers",
      columns: [
        {
          key: "name",
          label: "Name",
        },
        {
          key: "email",
          label: "Email",
        },
        {
          key: "kind",
          label: "Type",
        },
      ],
      fields: [
        {
          key: "name",
          label: "Name",
          type: "text",
        },
        {
          key: "email",
          label: "Email",
          type: "email",
        },
        {
          key: "kind",
          label: "Contact type",
          type: "select",
          options: ["Customer", "Supplier"],
        },
      ],
      createRoles: ["Admin", "Staff"],
      editRoles: ["Admin", "Staff"],
      deleteRoles: ["Admin"],
      filter: "kind",
    },
    {
      key: "orders",
      title: "Orders & invoices",
      columns: [
        {
          key: "id",
          label: "Order #",
        },
        {
          key: "kind",
          label: "Type",
        },
        {
          key: "party",
          label: "Contact",
        },
        {
          key: "createdAt",
          label: "Created",
        },
        {
          key: "total",
          label: "Total",
        },
        {
          key: "status",
          label: "Status",
        },
      ],
      fields: [
        {
          key: "kind",
          label: "Order type",
          type: "select",
          options: ["Sale", "Purchase"],
        },
        {
          key: "partyId",
          label: "Customer / supplier",
          type: "select",
          source: "parties",
          display: "name",
        },
      ],
      createRoles: ["Admin", "Staff"],
      editRoles: ["Admin", "Staff"],
      filter: "status",
    },
    {
      key: "movements",
      title: "Stock movements",
      columns: [
        {
          key: "product",
          label: "Product",
        },
        {
          key: "delta",
          label: "Change",
        },
        {
          key: "reason",
          label: "Reason",
        },
        {
          key: "orderId",
          label: "Order #",
        },
        {
          key: "at",
          label: "Timestamp",
        },
      ],
      fields: [],
    },
    {
      key: "users",
      title: "User accounts",
      columns: [
        {
          key: "name",
          label: "Name",
        },
        {
          key: "email",
          label: "Email",
        },
        {
          key: "role",
          label: "Role",
        },
        {
          key: "active",
          label: "Active",
        },
      ],
      fields: [
        {
          key: "name",
          label: "Name",
          type: "text",
        },
        {
          key: "email",
          label: "Email",
          type: "email",
        },
        {
          key: "password",
          label: "Initial password",
          type: "password",
          minlength: 12,
        },
        {
          key: "role",
          label: "Role",
          type: "select",
          options: ["Admin", "Staff"],
        },
      ],
      roles: ["Admin"],
      createRoles: ["Admin"],
    },
    {
      key: "audit",
      title: "Audit history",
      columns: [
        {
          key: "actor",
          label: "Actor",
        },
        {
          key: "action",
          label: "Action",
        },
        {
          key: "detail",
          label: "Details",
        },
        {
          key: "at",
          label: "Timestamp",
        },
      ],
      fields: [],
      roles: ["Admin"],
    },
  ],
};
