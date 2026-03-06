/*
 * SPDX-License-Identifier: Apache-2.0
 */

'use strict';

const ehrContract = require('./lib/ehrContract');

module.exports.EHRContract = ehrContract;
module.exports.contracts = [ehrContract];
