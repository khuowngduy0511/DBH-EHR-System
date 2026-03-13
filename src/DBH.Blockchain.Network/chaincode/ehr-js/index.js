'use strict';

const EhrContract = require('./lib/ehrContract');
const EmergencyContract = require('./lib/emergencyContract');

module.exports.EhrContract = EhrContract;
module.exports.EmergencyContract = EmergencyContract;
module.exports.contracts = [EhrContract, EmergencyContract];
