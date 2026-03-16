'use strict';

const EhrContract = require('./lib/ehrContract');
const EmergencyContract = require('./lib/emergencyContract');
const ConsentContract = require('./lib/consentContract');
const AuditContract = require('./lib/auditContract');

module.exports.EhrContract = EhrContract;
module.exports.EmergencyContract = EmergencyContract;
module.exports.ConsentContract = ConsentContract;
module.exports.AuditContract = AuditContract;
module.exports.contracts = [
  EhrContract,
  EmergencyContract,
  ConsentContract,
  AuditContract,
];
