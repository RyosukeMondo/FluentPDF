/**
 * Dummy form field generator for UI prototyping.
 * Creates realistic form field data with validation constraints.
 */

import { PdfFormField, FormFieldType, PdfRectangle } from '../types/models';

/**
 * Configuration for dummy form field generation.
 */
export interface DummyFormFieldConfig {
  pageNumber?: number;
  includeValidation?: boolean;
  includeRadioGroups?: boolean;
}

/**
 * Generates a dummy rectangle for a form field.
 */
function generateBounds(x: number, y: number, width: number, height: number): PdfRectangle {
  return {
    left: x,
    bottom: y,
    right: x + width,
    top: y + height,
  };
}

/**
 * Generates a single text field.
 */
export function generateTextField(
  name: string,
  pageNumber: number,
  y: number,
  config: Partial<DummyFormFieldConfig> = {}
): PdfFormField {
  const { includeValidation = true } = config;

  return {
    name,
    type: FormFieldType.Text,
    pageNumber,
    bounds: generateBounds(100, y, 200, 25),
    tabOrder: 0,
    value: '',
    isRequired: includeValidation && Math.random() > 0.7,
    isReadOnly: false,
    maxLength: includeValidation ? 100 : undefined,
  };
}

/**
 * Generates a single checkbox field.
 */
export function generateCheckboxField(
  name: string,
  pageNumber: number,
  y: number
): PdfFormField {
  return {
    name,
    type: FormFieldType.Checkbox,
    pageNumber,
    bounds: generateBounds(100, y, 20, 20),
    tabOrder: 0,
    isChecked: false,
    isRequired: false,
    isReadOnly: false,
  };
}

/**
 * Generates a radio button group.
 */
export function generateRadioGroup(
  groupName: string,
  options: string[],
  pageNumber: number,
  startY: number
): PdfFormField[] {
  return options.map((option, index) => ({
    name: `${groupName}_${option}`,
    type: FormFieldType.RadioButton,
    pageNumber,
    bounds: generateBounds(100, startY + index * 30, 20, 20),
    tabOrder: index,
    isChecked: index === 0, // First option selected by default
    isRequired: false,
    isReadOnly: false,
    groupName,
  }));
}

/**
 * Generates a combo box (dropdown) field.
 */
export function generateComboBoxField(
  name: string,
  pageNumber: number,
  y: number,
  options: string[]
): PdfFormField {
  return {
    name,
    type: FormFieldType.ComboBox,
    pageNumber,
    bounds: generateBounds(100, y, 200, 25),
    tabOrder: 0,
    value: options[0],
    isRequired: false,
    isReadOnly: false,
  };
}

/**
 * Generates a complete set of form fields for a typical form page.
 */
export function generateTypicalFormFields(
  pageNumber: number = 1,
  config: DummyFormFieldConfig = {}
): PdfFormField[] {
  const { includeValidation = true, includeRadioGroups = true } = config;

  const fields: PdfFormField[] = [];
  let yPosition = 700;

  // Personal Information Section
  fields.push(
    generateTextField('firstName', pageNumber, yPosition, { includeValidation }),
    generateTextField('lastName', pageNumber, yPosition - 40, { includeValidation }),
    generateTextField('email', pageNumber, yPosition - 80, {
      includeValidation,
    })
  );

  // Add email validation pattern
  if (includeValidation) {
    fields[2]!.formatMask = '^[^@]+@[^@]+\\.[^@]+$';
    fields[2]!.isRequired = true;
  }

  yPosition -= 140;

  // Address Section
  fields.push(
    generateTextField('address', pageNumber, yPosition, { includeValidation }),
    generateTextField('city', pageNumber, yPosition - 40, { includeValidation }),
    generateComboBoxField('state', pageNumber, yPosition - 80, [
      'CA',
      'NY',
      'TX',
      'FL',
      'WA',
    ]),
    generateTextField('zipCode', pageNumber, yPosition - 120, { includeValidation })
  );

  // Add ZIP validation pattern
  if (includeValidation) {
    fields[fields.length - 1]!.formatMask = '^\\d{5}(-\\d{4})?$';
    fields[fields.length - 1]!.maxLength = 10;
  }

  yPosition -= 180;

  // Radio button group for contact preference
  if (includeRadioGroups) {
    const radioGroup = generateRadioGroup(
      'contactPreference',
      ['Email', 'Phone', 'Mail'],
      pageNumber,
      yPosition
    );
    fields.push(...radioGroup);
    yPosition -= 100;
  }

  // Checkboxes for terms and conditions
  fields.push(
    generateCheckboxField('agreeToTerms', pageNumber, yPosition),
    generateCheckboxField('subscribeNewsletter', pageNumber, yPosition - 30)
  );

  if (includeValidation) {
    fields[fields.length - 2]!.isRequired = true; // Terms checkbox must be checked
  }

  // Update tab order
  fields.forEach((field, index) => {
    field.tabOrder = index;
  });

  return fields;
}

/**
 * Generates form fields with validation errors (for testing error states).
 */
export function generateFormFieldsWithErrors(pageNumber: number = 1): PdfFormField[] {
  const fields = generateTypicalFormFields(pageNumber, { includeValidation: true });

  // Set some invalid values
  fields[2]!.value = 'invalid-email'; // Email without @
  fields[fields.length - 3]!.value = 'ABCDE'; // ZIP code with letters
  fields[fields.length - 2]!.isChecked = false; // Required checkbox unchecked

  return fields;
}

/**
 * Generates a simple contact form (3-5 fields).
 */
export function generateSimpleContactForm(pageNumber: number = 1): PdfFormField[] {
  return [
    generateTextField('name', pageNumber, 700, { includeValidation: true }),
    generateTextField('email', pageNumber, 660, { includeValidation: true }),
    generateTextField('phone', pageNumber, 620, { includeValidation: false }),
    {
      ...generateTextField('message', pageNumber, 580, { includeValidation: false }),
      bounds: generateBounds(100, 500, 400, 80), // Larger text area
      maxLength: 500,
    },
    generateCheckboxField('agreeToPrivacyPolicy', pageNumber, 450),
  ];
}

/**
 * Generates a tax form (complex with many fields).
 */
export function generateTaxForm(pageNumber: number = 1): PdfFormField[] {
  const fields: PdfFormField[] = [];
  let yPosition = 750;

  // Taxpayer information
  fields.push(
    generateTextField('ssn', pageNumber, yPosition, { includeValidation: true }),
    generateTextField('taxpayerName', pageNumber, yPosition - 40, {
      includeValidation: true,
    }),
    generateTextField('spouseName', pageNumber, yPosition - 80, {
      includeValidation: false,
    })
  );

  // Add SSN validation
  fields[0]!.formatMask = '^\\d{3}-\\d{2}-\\d{4}$';
  fields[0]!.maxLength = 11;
  fields[0]!.isRequired = true;

  yPosition -= 140;

  // Income fields
  const incomeFields = [
    'wages',
    'interest',
    'dividends',
    'capitalGains',
    'businessIncome',
    'otherIncome',
  ];

  incomeFields.forEach((fieldName) => {
    fields.push(generateTextField(fieldName, pageNumber, yPosition, { includeValidation: true }));
    fields[fields.length - 1]!.formatMask = '^\\d+(\\.\\d{2})?$'; // Currency format
    yPosition -= 35;
  });

  yPosition -= 20;

  // Filing status radio group
  const filingStatus = generateRadioGroup(
    'filingStatus',
    ['Single', 'MarriedJoint', 'MarriedSeparate', 'HeadOfHousehold'],
    pageNumber,
    yPosition
  );
  fields.push(...filingStatus);

  yPosition -= 150;

  // Deduction checkboxes
  fields.push(
    generateCheckboxField('standardDeduction', pageNumber, yPosition),
    generateCheckboxField('itemizedDeduction', pageNumber, yPosition - 30)
  );

  // Update tab order
  fields.forEach((field, index) => {
    field.tabOrder = index;
  });

  return fields;
}

/**
 * Pre-configured form field sets for different scenarios.
 */
export const FormPresets = {
  /**
   * Empty page with no form fields.
   */
  empty: (): PdfFormField[] => [],

  /**
   * Simple contact form (3-5 fields).
   */
  simple: generateSimpleContactForm(1),

  /**
   * Typical form with all field types.
   */
  typical: generateTypicalFormFields(1, {
    includeValidation: true,
    includeRadioGroups: true,
  }),

  /**
   * Form with validation errors (for testing error display).
   */
  withErrors: generateFormFieldsWithErrors(1),

  /**
   * Complex tax form with many fields.
   */
  complex: generateTaxForm(1),

  /**
   * Read-only form (all fields disabled).
   */
  readOnly: generateTypicalFormFields(1).map((field) => ({
    ...field,
    isReadOnly: true,
    value: field.type === FormFieldType.Text ? 'Sample Value' : field.value,
    isChecked: field.type === FormFieldType.Checkbox ? true : field.isChecked,
  })),

  /**
   * Form with all required fields.
   */
  allRequired: generateTypicalFormFields(1, { includeValidation: true }).map((field) => ({
    ...field,
    isRequired: true,
  })),
};
