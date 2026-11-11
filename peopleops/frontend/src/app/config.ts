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
  slug: "peopleops",
  title: "PeopleOps",
  tag: "HR & leave management",
  color: "#6557df",
  description: "People, time, and thoughtful approvals.",
  primary: "leaves",
  pages: [
    {
      key: "employees",
      title: "Employees",
      columns: [
        {
          key: "name",
          label: "Employee",
        },
        {
          key: "email",
          label: "Email",
        },
        {
          key: "department",
          label: "Department",
        },
        {
          key: "jobTitle",
          label: "Job title",
        },
        {
          key: "remaining",
          label: "Leave balance",
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
          createOnly: true,
          minlength: 12,
        },
        {
          key: "departmentId",
          label: "Department",
          type: "select",
          source: "departments",
          display: "name",
        },
        {
          key: "jobTitle",
          label: "Job title",
          type: "text",
        },
        {
          key: "startDate",
          label: "Start date",
          type: "date",
        },
        {
          key: "annualAllowance",
          label: "Annual leave days",
          type: "number",
          min: 0,
          max: 60,
        },
      ],
      createRoles: ["Admin"],
      editRoles: ["Admin"],
    },
    {
      key: "departments",
      title: "Departments",
      columns: [
        {
          key: "name",
          label: "Department",
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
      key: "attendance",
      title: "Attendance",
      columns: [
        {
          key: "employee",
          label: "Employee",
        },
        {
          key: "day",
          label: "Day",
        },
        {
          key: "checkIn",
          label: "Check in",
        },
        {
          key: "checkOut",
          label: "Check out",
        },
        {
          key: "hours",
          label: "Hours",
        },
      ],
      fields: [],
    },
    {
      key: "leaves",
      title: "Leave requests",
      columns: [
        {
          key: "employee",
          label: "Employee",
        },
        {
          key: "start",
          label: "From",
        },
        {
          key: "end",
          label: "To",
        },
        {
          key: "days",
          label: "Days",
        },
        {
          key: "status",
          label: "Status",
        },
        {
          key: "reason",
          label: "Reason",
        },
        {
          key: "decisionNote",
          label: "Decision note",
        },
      ],
      fields: [
        {
          key: "employeeId",
          label: "Employee",
          type: "select",
          source: "employees",
          display: "name",
        },
        {
          key: "start",
          label: "From",
          type: "date",
        },
        {
          key: "end",
          label: "To",
          type: "date",
        },
        {
          key: "reason",
          label: "Reason",
          type: "textarea",
        },
      ],
      createRoles: ["Admin", "Employee"],
      filter: "status",
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
          options: ["Admin"],
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
