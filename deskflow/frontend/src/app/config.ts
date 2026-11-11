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
  slug: "deskflow",
  title: "DeskFlow",
  tag: "Support & service desk",
  color: "#2876bd",
  description: "Every request deserves a clear next step.",
  primary: "tickets",
  pages: [
    {
      key: "tickets",
      title: "Tickets",
      columns: [
        {
          key: "id",
          label: "Ticket #",
        },
        {
          key: "title",
          label: "Subject",
        },
        {
          key: "requester",
          label: "Requester",
        },
        {
          key: "priority",
          label: "Priority",
        },
        {
          key: "assignedTo",
          label: "Assigned to",
        },
        {
          key: "status",
          label: "Status",
        },
      ],
      fields: [
        {
          key: "title",
          label: "Subject",
          type: "text",
        },
        {
          key: "description",
          label: "Description",
          type: "textarea",
          max: 4000,
        },
        {
          key: "categoryId",
          label: "Category",
          type: "select",
          source: "categories",
          display: "name",
        },
        {
          key: "priority",
          label: "Priority",
          type: "select",
          options: ["Low", "Medium", "High", "Urgent"],
        },
      ],
      createRoles: ["Admin", "Staff", "Customer"],
      editRoles: ["Admin", "Staff", "Customer"],
      filter: "status",
    },
    {
      key: "categories",
      title: "Categories",
      columns: [
        {
          key: "name",
          label: "Category",
        },
      ],
      fields: [
        {
          key: "name",
          label: "Name",
          type: "text",
        },
      ],
      roles: ["Admin"],
      createRoles: ["Admin"],
      editRoles: ["Admin"],
      deleteRoles: ["Admin"],
    },
    {
      key: "performance",
      title: "Agent performance",
      columns: [
        {
          key: "name",
          label: "Agent",
        },
        {
          key: "assigned",
          label: "Assigned tickets",
        },
        {
          key: "resolved",
          label: "Resolved / closed",
        },
        {
          key: "averageHours",
          label: "Average resolution hours",
        },
      ],
      fields: [],
      roles: ["Admin", "Staff"],
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
          options: ["Admin", "Staff", "Customer"],
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
